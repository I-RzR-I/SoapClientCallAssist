import org.w3c.dom.*;
import javax.xml.parsers.*;
import javax.xml.crypto.dsig.*;
import javax.xml.crypto.dsig.dom.DOMValidateContext;
import javax.xml.crypto.dsig.keyinfo.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.KeyStore;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.cert.CertificateFactory;
import java.security.cert.X509Certificate;
import java.util.*;
import javax.crypto.Cipher;
import javax.crypto.Mac;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;

public class JavaVerify {

    static final String WSU = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    static final String WSSE = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    static final String DSIG = "http://www.w3.org/2000/09/xmldsig#";
    static final String SAML11 = "urn:oasis:names:tc:SAML:1.0:assertion";
    static final String SAML20 = "urn:oasis:names:tc:SAML:2.0:assertion";
    static final String XENC = "http://www.w3.org/2001/04/xmlenc#";
    static final String RSA_OAEP_MGF1P = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";
    static final String DEFAULT_LABEL = "WS-SecureConversationWS-SecureConversation";
    static final String SYMMETRIC_SIDECAR = ".symmetric.properties";

    public static void main(String[] args) throws Exception {
        Path dir = Paths.get(args[0]);
        boolean dumpC14n = args.length > 1 && args[1].equals("--dump");

        X509Certificate cert;
        try (InputStream in = Files.newInputStream(dir.resolve("signer.cer"))) {
            cert = (X509Certificate) CertificateFactory.getInstance("X.509").generateCertificate(in);
        }
        PublicKey trustedKey = cert.getPublicKey();
        System.out.println("Trusted key taken from signer.cer (NOT from the message KeyInfo)");
        System.out.println("  subject   : " + cert.getSubjectX500Principal());
        System.out.println("  algorithm : " + trustedKey.getAlgorithm() + "  format=" + trustedKey.getFormat());
        System.out.println();

        List<Path> files = new ArrayList<>();
        try (DirectoryStream<Path> ds = Files.newDirectoryStream(dir, "*.xml")) {
            for (Path p : ds) files.add(p);
        }
        Collections.sort(files);

        for (Path f : files) {
            String name = f.getFileName().toString();
            Path sidecar = dir.resolve(name.substring(0, name.length() - ".xml".length()) + SYMMETRIC_SIDECAR);
            if (Files.exists(sidecar)) verifySymmetric(f, sidecar, dumpC14n);
            else verifyOne(f, trustedKey, dumpC14n);
        }
    }

    static void verifyOne(Path file, PublicKey trustedKey, boolean dumpC14n) {
        String name = file.getFileName().toString();
        System.out.println("============================================================");
        System.out.println("CASE " + name);
        try {
            DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
            dbf.setNamespaceAware(true);
            dbf.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
            dbf.setFeature("http://xml.org/sax/features/external-general-entities", false);
            dbf.setFeature("http://xml.org/sax/features/external-parameter-entities", false);
            dbf.setXIncludeAware(false);
            dbf.setExpandEntityReferences(false);
            Document doc = dbf.newDocumentBuilder().parse(file.toFile());

            NodeList sigs = doc.getElementsByTagNameNS(DSIG, "Signature");
            if (sigs.getLength() == 0) { System.out.println("  NO ds:Signature FOUND"); return; }
            Element sigEl = messageSignature(sigs);

            DOMValidateContext ctx = new DOMValidateContext(trustedKey, sigEl);
            ctx.setProperty("javax.xml.crypto.dsig.cacheReference", Boolean.TRUE);

            int ids = registerWsuIds(doc.getDocumentElement(), ctx);
            System.out.println("  wsu:Id attributes registered as XML IDs: " + ids);
            int samlIds = registerSamlIds(doc.getDocumentElement(), ctx);
            System.out.println("  SAML identifier attributes registered as XML IDs: " + samlIds);

            XMLSignatureFactory fac = XMLSignatureFactory.getInstance("DOM");
            XMLSignature sig = fac.unmarshalXMLSignature(ctx);

            SignedInfo si = sig.getSignedInfo();
            System.out.println("  CanonicalizationMethod : " + si.getCanonicalizationMethod().getAlgorithm());
            System.out.println("  SignatureMethod        : " + si.getSignatureMethod().getAlgorithm());

            Boolean core = null;
            String coreErr = null;
            try { core = sig.validate(ctx); }
            catch (Exception e) { coreErr = describe(e); }

            System.out.println("  CORE VALIDITY          : " + verdict(core, coreErr));

            Boolean svValid = null;
            String svErr = null;
            try { svValid = sig.getSignatureValue().validate(ctx); }
            catch (Exception e) { svErr = describe(e); }
            System.out.println("  SignedInfo/SignatureValue validity : " + verdict(svValid, svErr));

            @SuppressWarnings("unchecked")
            List<Reference> refs = si.getReferences();
            for (Reference r : refs) {
                Boolean ok = null;
                String err = null;
                try { ok = r.validate(ctx); }
                catch (Exception e) { err = describe(e); }
                System.out.println("    Reference URI=" + r.getURI());
                System.out.println("      digestMethod  : " + r.getDigestMethod().getAlgorithm());
                System.out.println("      DIGEST VALID  : " + verdict(ok, err));
                System.out.println("      expected(msg) : " + b64(r.getDigestValue()));
                System.out.println("      calculated    : " + b64(r.getCalculatedDigestValue()));
                Object deref = r.getDereferencedData();
                System.out.println("      dereferenced  : " + (deref == null ? "NULL  <-- reference did NOT resolve" : "resolved (" + deref.getClass().getSimpleName() + ")"));
                if (dumpC14n) {
                    try (InputStream dis = r.getDigestInputStream()) {
                        if (dis == null) System.out.println("      digest input  : <not cached>");
                        else System.out.println("      JAVA digest input bytes:" + System.lineSeparator() + "      >>>" + readAll(dis) + "<<<");
                    } catch (Exception e) { System.out.println("      digest input dump failed: " + e); }
                }
            }

            if (dumpC14n) {
                try (InputStream in = si.getCanonicalizedData()) {
                    if (in == null) System.out.println("  canonicalized SignedInfo: <not available>");
                    else {
                        ByteArrayOutputStream bos = new ByteArrayOutputStream();
                        byte[] buf = new byte[8192]; int n;
                        while ((n = in.read(buf)) > 0) bos.write(buf, 0, n);
                        byte[] c = bos.toByteArray();
                        System.out.println("  JAVA canonicalized SignedInfo (" + c.length + " bytes):");
                        System.out.println("  >>>" + new String(c, StandardCharsets.UTF_8) + "<<<");
                    }
                } catch (Exception e) {
                    System.out.println("  canonicalized SignedInfo dump failed: " + e);
                }
            }
        } catch (Exception e) {
            System.out.println("  HARNESS ERROR: " + e);
            e.printStackTrace(System.out);
        }
        System.out.println();
    }

    static String verdict(Boolean value, String error) {
        if (error != null) return "ERROR   (" + error + ")";
        if (value == null) return "ERROR   (no verdict was produced and no exception was reported)";
        return value ? "VALID" : "INVALID";
    }

    static String describe(Throwable t) {
        StringBuilder sb = new StringBuilder(t.getClass().getName() + ": " + t.getMessage());
        Throwable cause = t.getCause();
        int depth = 0;
        while (cause != null && depth < 4) {
            sb.append(" <- ").append(cause.getClass().getName()).append(": ").append(cause.getMessage());
            cause = cause.getCause();
            depth++;
        }
        return sb.toString();
    }

    static String readAll(InputStream in) throws IOException {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        byte[] buf = new byte[8192]; int n;
        while ((n = in.read(buf)) > 0) bos.write(buf, 0, n);
        return new String(bos.toByteArray(), StandardCharsets.UTF_8);
    }

    static int registerWsuIds(Element el, DOMValidateContext ctx) {
        int count = 0;
        Attr id = el.getAttributeNodeNS(WSU, "Id");
        if (id != null) {
            ctx.setIdAttributeNS(el, WSU, "Id");
            count++;
        }
        NodeList kids = el.getChildNodes();
        for (int i = 0; i < kids.getLength(); i++) {
            Node n = kids.item(i);
            if (n.getNodeType() == Node.ELEMENT_NODE) count += registerWsuIds((Element) n, ctx);
        }
        return count;
    }

    static Element messageSignature(NodeList sigs) {
        for (int i = 0; i < sigs.getLength(); i++) {
            Element candidate = (Element) sigs.item(i);
            Node parent = candidate.getParentNode();
            if (parent != null && parent.getNodeType() == Node.ELEMENT_NODE
                    && WSSE.equals(parent.getNamespaceURI()) && "Security".equals(parent.getLocalName()))
                return candidate;
        }
        return (Element) sigs.item(0);
    }

    static int registerSamlIds(Element el, DOMValidateContext ctx) {
        int count = 0;
        if ("Assertion".equals(el.getLocalName())) {
            String attribute = SAML20.equals(el.getNamespaceURI()) ? "ID" : SAML11.equals(el.getNamespaceURI()) ? "AssertionID" : null;
            if (attribute != null && el.getAttributeNode(attribute) != null) {
                ctx.setIdAttributeNS(el, null, attribute);
                count++;
            }
        }
        NodeList kids = el.getChildNodes();
        for (int i = 0; i < kids.getLength(); i++) {
            Node n = kids.item(i);
            if (n.getNodeType() == Node.ELEMENT_NODE) count += registerSamlIds((Element) n, ctx);
        }
        return count;
    }

    static String b64(byte[] b) { return b == null ? "<null>" : Base64.getEncoder().encodeToString(b); }

    static final class SymmetricVerdict extends Exception {
        private static final long serialVersionUID = 1L;
        final boolean error;
        SymmetricVerdict(boolean error, String reason) { super(reason); this.error = error; }
        String verdictText() { return error ? "ERROR   (" + getMessage() + ")" : "INVALID (" + getMessage() + ")"; }
    }

    static void verifySymmetric(Path file, Path sidecar, boolean dumpC14n) {
        String name = file.getFileName().toString();
        System.out.println("============================================================");
        System.out.println("CASE " + name);
        System.out.println("  MODE                   : symmetric (EncryptedKey unwrap, P_SHA1 derivation, HMAC validation; nothing shared with the library)");
        System.out.println("  JDK                    : " + System.getProperty("java.version") + " (" + System.getProperty("java.vendor") + ") at " + System.getProperty("java.home"));
        try {
            Properties expected = new Properties();
            try (Reader reader = Files.newBufferedReader(sidecar, StandardCharsets.UTF_8)) { expected.load(reader); }

            DocumentBuilder builder = hardenedBuilder();
            Document doc = builder.parse(file.toFile());
            Element security = securityHeader(doc);
            if (security == null) throw new SymmetricVerdict(true, "no wsse:Security header was found");

            byte[] secret = unwrapSecret(security, sidecar.getParent(), expected);

            Map<String, byte[]> keys = deriveKeys(security, secret);

            decryptEncryptedData(doc, builder, security, keys);

            reportUsernameToken(security, expected.getProperty("username"));

            List<Element> signatures = childElements(security, DSIG, "Signature");
            Element primary = null;
            Element endorsing = null;
            for (Element candidate : signatures) {
                if (keys.containsKey(keyReferenceId(candidate))) primary = candidate;
                else endorsing = candidate;
            }
            if (primary == null) throw new SymmetricVerdict(true, "no ds:Signature whose KeyInfo references a DerivedKeyToken was found among " + signatures.size() + " signature(s)");

            String primaryId = primary.getAttribute("Id");
            String primaryKeyId = keyReferenceId(primary);
            System.out.println("  PRIMARY SIGNATURE      : Id=" + (primaryId.isEmpty() ? "-" : primaryId) + " keyed by DerivedKeyToken " + primaryKeyId);

            Element signatureMethod = firstDescendant(primary, DSIG, "SignatureMethod");
            String signatureAlgorithm = signatureMethod == null ? "" : signatureMethod.getAttribute("Algorithm");
            Element outputLength = signatureMethod == null ? null : firstChild(signatureMethod, DSIG, "HMACOutputLength");
            String macName = macNameOf(signatureAlgorithm);

            DOMValidateContext ctx = new DOMValidateContext(new SecretKeySpec(keys.get(primaryKeyId), macName == null ? "HmacSHA256" : macName), primary);
            ctx.setProperty("javax.xml.crypto.dsig.cacheReference", Boolean.TRUE);
            int ids = registerWsuIds(doc.getDocumentElement(), ctx);
            System.out.println("  wsu:Id attributes registered as XML IDs: " + ids);
            for (Element candidate : signatures) if (candidate.hasAttribute("Id")) ctx.setIdAttributeNS(candidate, null, "Id");

            XMLSignatureFactory fac = XMLSignatureFactory.getInstance("DOM");
            XMLSignature sig = fac.unmarshalXMLSignature(ctx);
            SignedInfo si = sig.getSignedInfo();
            System.out.println("  CanonicalizationMethod : " + si.getCanonicalizationMethod().getAlgorithm());
            System.out.println("  SignatureMethod        : " + si.getSignatureMethod().getAlgorithm());
            System.out.println("  HMAC                   : " + (macName == null ? "NOT an HMAC SignatureMethod" : macName + " under DerivedKeyToken " + primaryKeyId + " (" + keys.get(primaryKeyId).length + " bytes)"));
            System.out.println("  HMACOutputLength       : " + (outputLength == null ? "absent" : "present (" + outputLength.getTextContent().trim() + ")"));

            if (macName == null) throw new SymmetricVerdict(false, "SignatureMethod " + signatureAlgorithm + " is not an HMAC, so a DerivedKeyToken cannot key it");
            if (outputLength != null) throw new SymmetricVerdict(false, "HMACOutputLength is refused: a truncated MAC weakens the signature");

            Boolean core = null;
            String coreErr = null;
            try { core = sig.validate(ctx); }
            catch (Exception e) { coreErr = describe(e); }
            System.out.println("  CORE VALIDITY          : " + verdict(core, coreErr));

            Boolean svValid = null;
            String svErr = null;
            try { svValid = sig.getSignatureValue().validate(ctx); }
            catch (Exception e) { svErr = describe(e); }
            System.out.println("  SignedInfo/SignatureValue validity : " + verdict(svValid, svErr));

            printReferences(si, ctx, dumpC14n);

            if (endorsing != null) verifyEndorsing(doc, security, endorsing, primary, fac);
            else System.out.println("  ENDORSING SIGNATURE    : none");
        } catch (SymmetricVerdict v) {
            System.out.println("  CORE VALIDITY          : " + v.verdictText());
            System.out.println("  SignedInfo/SignatureValue validity : " + v.verdictText());
        } catch (Exception e) {
            System.out.println("  HARNESS ERROR: " + e);
            e.printStackTrace(System.out);
        }
        System.out.println();
    }

    static byte[] unwrapSecret(Element security, Path dir, Properties expected) throws SymmetricVerdict {
        List<Element> encryptedKeys = childElements(security, XENC, "EncryptedKey");
        System.out.println("  EncryptedKey count     : " + encryptedKeys.size());
        if (encryptedKeys.size() != 1) throw new SymmetricVerdict(true, "expected exactly one xenc:EncryptedKey under wsse:Security, found " + encryptedKeys.size());
        Element encryptedKey = encryptedKeys.get(0);

        String algorithm = encryptionMethodAlgorithm(encryptedKey);
        System.out.println("  EncryptedKey/EncryptionMethod : " + algorithm);
        if (!RSA_OAEP_MGF1P.equals(algorithm)) {
            System.out.println("  KEY UNWRAP             : INVALID (EncryptionMethod " + algorithm + " is not " + RSA_OAEP_MGF1P + ")");
            throw new SymmetricVerdict(false, "EncryptionMethod " + algorithm + " is not " + RSA_OAEP_MGF1P);
        }

        try {
            PrivateKey serviceKey = loadPrivateKey(dir.resolve(expected.getProperty("keystore")), expected.getProperty("password"));
            byte[] wrapped = Base64.getDecoder().decode(cipherValue(encryptedKey));
            Cipher rsa = Cipher.getInstance("RSA/ECB/OAEPWithSHA-1AndMGF1Padding");
            rsa.init(Cipher.DECRYPT_MODE, serviceKey);
            byte[] secret = rsa.doFinal(wrapped);
            System.out.println("  KEY UNWRAP             : VALID (" + secret.length + "-byte secret from a " + wrapped.length + "-byte CipherValue via RSA/ECB/OAEPWithSHA-1AndMGF1Padding)");
            return secret;
        } catch (Exception e) {
            System.out.println("  KEY UNWRAP             : " + verdict(null, describe(e)));
            throw new SymmetricVerdict(true, "the EncryptedKey could not be unwrapped with the service private key: " + describe(e));
        }
    }

    static PrivateKey loadPrivateKey(Path keystorePath, String password) throws Exception {
        KeyStore store = KeyStore.getInstance("PKCS12");
        try (InputStream in = Files.newInputStream(keystorePath)) { store.load(in, password.toCharArray()); }
        for (Enumeration<String> aliases = store.aliases(); aliases.hasMoreElements();) {
            String alias = aliases.nextElement();
            if (store.isKeyEntry(alias)) return (PrivateKey) store.getKey(alias, password.toCharArray());
        }
        throw new IllegalStateException("the keystore '" + keystorePath.getFileName() + "' holds no private key entry");
    }

    static Map<String, byte[]> deriveKeys(Element security, byte[] secret) throws Exception {
        List<Element> tokens = childElements(security, null, "DerivedKeyToken");
        if (tokens.isEmpty()) throw new SymmetricVerdict(true, "no DerivedKeyToken was found under wsse:Security");
        String encryptedKeyId = childElements(security, XENC, "EncryptedKey").get(0).getAttribute("Id");

        Map<String, byte[]> keys = new LinkedHashMap<>();
        for (Element token : tokens) {
            String ns = token.getNamespaceURI();
            String id = token.getAttributeNS(WSU, "Id");
            String label = childText(token, ns, "Label");
            if (label == null) label = DEFAULT_LABEL;
            String nonceText = childText(token, ns, "Nonce");
            if (nonceText == null) throw new SymmetricVerdict(true, "DerivedKeyToken " + id + " carries no Nonce");
            byte[] nonce = Base64.getDecoder().decode(nonceText);
            int length = childText(token, ns, "Length") == null ? 32 : Integer.parseInt(childText(token, ns, "Length").trim());
            int offset = childText(token, ns, "Offset") == null ? 0 : Integer.parseInt(childText(token, ns, "Offset").trim());
            String generation = childText(token, ns, "Generation");
            if (generation != null) offset = Integer.parseInt(generation.trim()) * length;
            String keyed = keyReferenceId(token);
            boolean keyedToOurs = keyed.equals(encryptedKeyId);
            if (!keyedToOurs) throw new SymmetricVerdict(false, "DerivedKeyToken " + id + " references '" + keyed + "', not this message's EncryptedKey '" + encryptedKeyId + "'");

            byte[] seed = concat(label.getBytes(StandardCharsets.UTF_8), nonce);
            byte[] stream = pSha1(secret, seed, offset + length);
            byte[] key = Arrays.copyOfRange(stream, offset, offset + length);
            keys.put(id, key);
            System.out.println("  DERIVED KEY            : id=" + id + " namespace=" + ns + " label=" + label + " offset=" + offset + " length=" + length + " generation=" + (generation == null ? "-" : generation.trim()) + " nonce=" + nonceText.trim() + " key=" + b64(key));
        }
        return keys;
    }

    static byte[] pSha1(byte[] secret, byte[] seed, int length) throws Exception {
        Mac mac = Mac.getInstance("HmacSHA1");
        mac.init(new SecretKeySpec(secret, "HmacSHA1"));
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        byte[] a = seed;
        while (out.size() < length) {
            a = mac.doFinal(a);
            mac.update(a);
            out.write(mac.doFinal(seed));
        }
        return Arrays.copyOf(out.toByteArray(), length);
    }

    static void decryptEncryptedData(Document doc, DocumentBuilder builder, Element security, Map<String, byte[]> keys) {
        for (Element encryptedData : childElements(security, XENC, "EncryptedData")) {
            String id = encryptedData.getAttribute("Id");
            String algorithm = encryptionMethodAlgorithm(encryptedData);
            String keyId = keyReferenceId(encryptedData);
            String line = "  DECRYPTION Id=" + id + " : ";
            try {
                byte[] key = keys.get(keyId);
                if (key == null) throw new IllegalStateException("KeyInfo references DerivedKeyToken '" + keyId + "', which the header does not carry");
                int expectedKeyLength = aesKeyLength(algorithm);
                if (key.length != expectedKeyLength) throw new IllegalStateException(algorithm + " needs a " + expectedKeyLength + "-byte key but DerivedKeyToken " + keyId + " derives " + key.length + " bytes");
                byte[] cipherValue = Base64.getDecoder().decode(cipherValue(encryptedData));
                Cipher aes = Cipher.getInstance("AES/CBC/ISO10126Padding");
                aes.init(Cipher.DECRYPT_MODE, new SecretKeySpec(key, "AES"), new IvParameterSpec(Arrays.copyOf(cipherValue, 16)));
                byte[] plaintext = aes.doFinal(cipherValue, 16, cipherValue.length - 16);
                Document plain = builder.parse(new ByteArrayInputStream(plaintext));
                Element imported = (Element) doc.importNode(plain.getDocumentElement(), true);
                security.replaceChild(imported, encryptedData);
                System.out.println(line + "VALID (element=" + imported.getLocalName() + " namespace=" + imported.getNamespaceURI() + " algorithm=" + algorithm + " keyedBy=" + keyId + " plaintextBytes=" + plaintext.length + ")");
            } catch (Exception e) {
                System.out.println(line + verdict(null, describe(e)));
            }
        }
    }

    static void reportUsernameToken(Element security, String expectedUsername) {
        if (expectedUsername == null) return;
        Element token = firstChild(security, WSSE, "UsernameToken");
        if (token == null) {
            System.out.println("  USERNAME TOKEN         : INVALID (no wsse:UsernameToken is present in the header after decryption)");
            return;
        }
        String username = childText(token, WSSE, "Username");
        Element password = firstChild(token, WSSE, "Password");
        System.out.println("  USERNAME               : " + username);
        System.out.println("  USERNAME TOKEN Id      : " + token.getAttributeNS(WSU, "Id"));
        System.out.println("  USERNAME TOKEN PASSWORD: " + (password == null ? "absent" : "present, Type=" + password.getAttribute("Type") + " (value not printed)"));
        System.out.println("  USERNAME TOKEN         : " + (expectedUsername.equals(username)
                ? "VALID (username matches the expected '" + expectedUsername + "')"
                : "INVALID (username '" + username + "' is not the expected '" + expectedUsername + "')"));
    }

    static void verifyEndorsing(Document doc, Element security, Element endorsing, Element primary, XMLSignatureFactory fac) {
        try {
            String bstId = keyReferenceId(endorsing);
            Element bst = null;
            for (Element candidate : childElements(security, WSSE, "BinarySecurityToken"))
                if (bstId.equals(candidate.getAttributeNS(WSU, "Id"))) bst = candidate;
            if (bst == null) throw new IllegalStateException("the endorsing signature references BinarySecurityToken '" + bstId + "', which the header does not carry");

            X509Certificate cert;
            try (InputStream in = new ByteArrayInputStream(Base64.getDecoder().decode(bst.getTextContent().trim()))) {
                cert = (X509Certificate) CertificateFactory.getInstance("X.509").generateCertificate(in);
            }
            System.out.println("  ENDORSING KEY          : X509 public key from BinarySecurityToken " + bstId + " subject=" + cert.getSubjectX500Principal() + " algorithm=" + cert.getPublicKey().getAlgorithm());

            DOMValidateContext ctx = new DOMValidateContext(cert.getPublicKey(), endorsing);
            ctx.setProperty("javax.xml.crypto.dsig.cacheReference", Boolean.TRUE);
            registerWsuIds(doc.getDocumentElement(), ctx);
            if (primary.hasAttribute("Id")) ctx.setIdAttributeNS(primary, null, "Id");
            XMLSignature sig = fac.unmarshalXMLSignature(ctx);
            SignedInfo si = sig.getSignedInfo();
            System.out.println("  ENDORSING SignatureMethod : " + si.getSignatureMethod().getAlgorithm());

            Boolean core = null;
            String coreErr = null;
            try { core = sig.validate(ctx); }
            catch (Exception e) { coreErr = describe(e); }
            System.out.println("  ENDORSING CORE VALIDITY : " + verdict(core, coreErr));

            Boolean svValid = null;
            String svErr = null;
            try { svValid = sig.getSignatureValue().validate(ctx); }
            catch (Exception e) { svErr = describe(e); }
            System.out.println("  ENDORSING SignedInfo/SignatureValue validity : " + verdict(svValid, svErr));

            @SuppressWarnings("unchecked")
            List<Reference> refs = si.getReferences();
            System.out.println("  ENDORSING REFERENCE COUNT : " + refs.size());
            for (Reference r : refs) {
                Boolean ok = null;
                String err = null;
                try { ok = r.validate(ctx); }
                catch (Exception e) { err = describe(e); }
                Object deref = r.getDereferencedData();
                String primaryId = primary.getAttribute("Id");
                boolean targetsPrimary = !primaryId.isEmpty() && ("#" + primaryId).equals(r.getURI()) && deref != null;
                System.out.println("  ENDORSING REFERENCE URI : " + r.getURI());
                System.out.println("  ENDORSING DIGEST VALID : " + verdict(ok, err));
                System.out.println("  ENDORSING TARGET       : " + (targetsPrimary ? "primary signature" : "NOT the primary signature (URI " + r.getURI() + ", primary Id '" + primaryId + "', dereferenced=" + (deref != null) + ")"));
            }
        } catch (Exception e) {
            System.out.println("  ENDORSING CORE VALIDITY : " + verdict(null, describe(e)));
            System.out.println("  ENDORSING SignedInfo/SignatureValue validity : " + verdict(null, describe(e)));
        }
    }

    static void printReferences(SignedInfo si, DOMValidateContext ctx, boolean dumpC14n) {
        @SuppressWarnings("unchecked")
        List<Reference> refs = si.getReferences();
        for (Reference r : refs) {
            Boolean ok = null;
            String err = null;
            try { ok = r.validate(ctx); }
            catch (Exception e) { err = describe(e); }
            System.out.println("    Reference URI=" + r.getURI());
            System.out.println("      digestMethod  : " + r.getDigestMethod().getAlgorithm());
            System.out.println("      DIGEST VALID  : " + verdict(ok, err));
            System.out.println("      expected(msg) : " + b64(r.getDigestValue()));
            System.out.println("      calculated    : " + b64(r.getCalculatedDigestValue()));
            Object deref = r.getDereferencedData();
            System.out.println("      dereferenced  : " + (deref == null ? "NULL  <-- reference did NOT resolve" : "resolved (" + deref.getClass().getSimpleName() + ")"));
            if (dumpC14n) {
                try (InputStream dis = r.getDigestInputStream()) {
                    if (dis == null) System.out.println("      digest input  : <not cached>");
                    else System.out.println("      JAVA digest input bytes:" + System.lineSeparator() + "      >>>" + readAll(dis) + "<<<");
                } catch (Exception e) { System.out.println("      digest input dump failed: " + e); }
            }
        }
    }

    static DocumentBuilder hardenedBuilder() throws Exception {
        DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
        dbf.setNamespaceAware(true);
        dbf.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
        dbf.setFeature("http://xml.org/sax/features/external-general-entities", false);
        dbf.setFeature("http://xml.org/sax/features/external-parameter-entities", false);
        dbf.setXIncludeAware(false);
        dbf.setExpandEntityReferences(false);
        return dbf.newDocumentBuilder();
    }

    static Element securityHeader(Document doc) {
        NodeList headers = doc.getElementsByTagNameNS(WSSE, "Security");
        for (int i = 0; i < headers.getLength(); i++) {
            Element candidate = (Element) headers.item(i);
            Node parent = candidate.getParentNode();
            if (parent != null && "Header".equals(parent.getLocalName())) return candidate;
        }
        return null;
    }

    static List<Element> childElements(Element parent, String ns, String localName) {
        List<Element> found = new ArrayList<>();
        NodeList kids = parent.getChildNodes();
        for (int i = 0; i < kids.getLength(); i++) {
            Node n = kids.item(i);
            if (n.getNodeType() != Node.ELEMENT_NODE) continue;
            if (!localName.equals(n.getLocalName())) continue;
            if (ns != null && !ns.equals(n.getNamespaceURI())) continue;
            found.add((Element) n);
        }
        return found;
    }

    static Element firstChild(Element parent, String ns, String localName) {
        List<Element> found = childElements(parent, ns, localName);
        return found.isEmpty() ? null : found.get(0);
    }

    static String childText(Element parent, String ns, String localName) {
        Element child = firstChild(parent, ns, localName);
        return child == null ? null : child.getTextContent();
    }

    static Element firstDescendant(Element root, String ns, String localName) {
        NodeList found = root.getElementsByTagNameNS(ns, localName);
        return found.getLength() == 0 ? null : (Element) found.item(0);
    }

    static String encryptionMethodAlgorithm(Element encrypted) {
        Element method = firstChild(encrypted, XENC, "EncryptionMethod");
        return method == null ? "<absent>" : method.getAttribute("Algorithm");
    }

    static String cipherValue(Element encrypted) {
        Element cipherData = firstChild(encrypted, XENC, "CipherData");
        String value = cipherData == null ? null : childText(cipherData, XENC, "CipherValue");
        if (value == null) throw new IllegalStateException("no xenc:CipherData/xenc:CipherValue");
        return value.trim();
    }

    static String keyReferenceId(Element signatureOrToken) {
        Element keyInfo = firstChild(signatureOrToken, DSIG, "KeyInfo");
        Element str = firstChild(keyInfo == null ? signatureOrToken : keyInfo, WSSE, "SecurityTokenReference");
        Element reference = str == null ? null : firstChild(str, WSSE, "Reference");
        String uri = reference == null ? "" : reference.getAttribute("URI");
        return uri.startsWith("#") ? uri.substring(1) : uri;
    }

    static String macNameOf(String algorithm) {
        switch (algorithm) {
            case "http://www.w3.org/2000/09/xmldsig#hmac-sha1": return "HmacSHA1";
            case "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256": return "HmacSHA256";
            case "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384": return "HmacSHA384";
            case "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512": return "HmacSHA512";
            default: return null;
        }
    }

    static int aesKeyLength(String algorithm) {
        switch (algorithm) {
            case "http://www.w3.org/2001/04/xmlenc#aes128-cbc": return 16;
            case "http://www.w3.org/2001/04/xmlenc#aes192-cbc": return 24;
            case "http://www.w3.org/2001/04/xmlenc#aes256-cbc": return 32;
            default: throw new IllegalStateException("EncryptionMethod " + algorithm + " is not an AES-CBC algorithm this verifier decrypts");
        }
    }

    static byte[] concat(byte[] left, byte[] right) {
        byte[] joined = Arrays.copyOf(left, left.length + right.length);
        System.arraycopy(right, 0, joined, left.length, right.length);
        return joined;
    }
}
