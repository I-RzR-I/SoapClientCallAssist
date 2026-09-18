#nullable disable

using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropRun
{

    private JavaInteropRun(
        string skipReason,
        string failureReason,
        string output,
        IReadOnlyDictionary<string, JavaInteropCaseResult> cases,
        IReadOnlyDictionary<string, string> bodyReferenceUris,
        IReadOnlyDictionary<string, SymmetricJavaInteropExpectation> symmetric,
        JavaInteropToolchain toolchain)
    {
        SkipReason = skipReason;
        FailureReason = failureReason;
        Output = output;
        Cases = cases ?? new Dictionary<string, JavaInteropCaseResult>();
        BodyReferenceUris = bodyReferenceUris ?? new Dictionary<string, string>();
        Symmetric = symmetric ?? new Dictionary<string, SymmetricJavaInteropExpectation>();
        Toolchain = toolchain;
    }

    internal string SkipReason { get; }

    internal string FailureReason { get; }

    internal string Output { get; }

    internal IReadOnlyDictionary<string, JavaInteropCaseResult> Cases { get; }

    internal IReadOnlyDictionary<string, string> BodyReferenceUris { get; }

    internal IReadOnlyDictionary<string, SymmetricJavaInteropExpectation> Symmetric { get; }

    internal JavaInteropToolchain Toolchain { get; }

    internal static JavaInteropRun Skipped(string reason) => new(reason, null, null, null, null, null, null);

    internal static JavaInteropRun Failed(string reason, string output, JavaInteropToolchain toolchain)
        => new(null, reason, output, null, null, null, toolchain);

    internal static JavaInteropRun Completed(
        string output,
        IReadOnlyDictionary<string, JavaInteropCaseResult> cases,
        IReadOnlyDictionary<string, string> bodyReferenceUris,
        IReadOnlyDictionary<string, SymmetricJavaInteropExpectation> symmetric,
        JavaInteropToolchain toolchain)
        => new(null, null, output, cases, bodyReferenceUris, symmetric, toolchain);
}
