

namespace ShopFlow.Api.Entities;

[Flags]
public enum Connectivity
{
    None = 0,
    Wired = 1,
    Bluetooth = 2,
    Wireless2_4G = 4
}