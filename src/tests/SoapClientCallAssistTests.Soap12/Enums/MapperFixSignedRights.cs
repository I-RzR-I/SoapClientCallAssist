
#nullable disable

using System;

namespace SoapClientCallAssistTests.Soap12.Enums;

[Flags]
public enum MapperFixSignedRights
{
    None = 0,
    Low = 1,
    High = 2,
    Sign = int.MinValue
}
