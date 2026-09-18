#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Encryption;

[TestClass]
public sealed class TierBDecryptorIsolationTests
{

    private const string DecryptorTypeName = "SoapClientCallAssist.Security.WsSecurity.WsSecurityResponseDecryptor";

    private const string VerifierTypeName = "SoapClientCallAssist.Security.WsSecurity.WsSecuritySymmetricResponseVerifier";

    private static readonly Dictionary<short, OpCode> OpCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null))
        .ToDictionary(opCode => opCode.Value);

    [DataTestMethod]
    [DataRow(DecryptorTypeName)]
    [DataRow(VerifierTypeName)]
    public void ResponseSide_NeverCallsIntoEncryptedXml_Test(string typeName)
    {
        var type = WsSecurityFoundationTestSupport.LibraryType(typeName);
        var offenders = new List<string>();

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        foreach (var member in ReferencedMembers(method))
            if (member.DeclaringType == typeof(EncryptedXml) || member.DeclaringType == typeof(EncryptedData) || member.DeclaringType == typeof(EncryptedKey))
                offenders.Add($"{method.Name} -> {member.DeclaringType.Name}.{member.Name}");

        Assert.AreEqual(0, offenders.Count, $"{type.Name} | {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void Scanner_SeesTheRawAesTransformTheDecryptorUses_Test()
    {
        var type = WsSecurityFoundationTestSupport.LibraryType(DecryptorTypeName);

        var members = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .SelectMany(ReferencedMembers)
            .ToList();

        Assert.IsTrue(members.Any(member => member.DeclaringType == typeof(System.Security.Cryptography.Aes)));
        Assert.IsTrue(members.Any(member => member.DeclaringType == typeof(System.Security.Cryptography.SymmetricAlgorithm) && member.Name == nameof(System.Security.Cryptography.SymmetricAlgorithm.CreateDecryptor)));
    }

    private static IEnumerable<MemberInfo> ReferencedMembers(MethodInfo method)
    {
        var body = method.GetMethodBody();

        if (body is null)
            yield break;

        var il = body.GetILAsByteArray();
        var module = method.Module;
        var position = 0;

        while (position < il.Length)
        {
            var opCode = ReadOpCode(il, ref position);
            var operandSize = OperandSize(opCode, il, position);

            if (opCode.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineTok)
            {
                var token = BitConverter.ToInt32(il, position);
                MemberInfo member = null;

                try
                {
                    member = module.ResolveMember(token, GenericArguments(method.DeclaringType), GenericArguments(method));
                }
                catch (ArgumentException)
                {
                }

                if (member is not null)
                    yield return member;
            }

            position += operandSize;
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int position)
    {
        short value = il[position++];

        if (value == 0xFE)
            value = (short)(0xFE00 | il[position++]);

        return OpCodes[value];
    }

    private static int OperandSize(OpCode opCode, byte[] il, int position)
    {
        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            case OperandType.InlineSwitch:
                return 4 + 4 * BitConverter.ToInt32(il, position);
            default:
                return 4;
        }
    }

    private static Type[] GenericArguments(MemberInfo member)
        => member switch
        {
            Type type when type.IsGenericType => type.GetGenericArguments(),
            MethodInfo method when method.IsGenericMethod => method.GetGenericArguments(),
            _ => null
        };
}
