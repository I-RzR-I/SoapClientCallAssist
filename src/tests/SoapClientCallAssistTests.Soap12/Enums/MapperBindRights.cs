
#nullable disable

using System;

namespace SoapClientCallAssistTests.Soap12.Enums;

[Flags]
public enum MapperBindRights
{
    None = 0,
    Read = 1,
    Write = 2,
    Admin = 4
}
