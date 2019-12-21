using Config.Net;
using System.Collections.Generic;

namespace AlgoWallet
{
    public interface IAppSettings
    {
        string AlogApiAddress { get; set; }
        string AlgoApiToken { get; set; }
        string[] Accounts { get; set; }
        string[] Mnemonics { get; set; }
        string[] Passwords { get; set; }
    }
}
