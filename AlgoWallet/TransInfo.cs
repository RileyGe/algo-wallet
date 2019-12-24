using Algorand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlgoWallet
{
    public class TransInfo
    {
        private Algorand.Algod.Client.Model.Transaction _trans;
        private string accountAddress;
        public string TxID { get {
                return _trans.Tx;
            } 
        }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public string Type { get {
                return _trans.Type;
            } }
        public string Amount { 
            get 
            {
                if (_trans.Type == "pay")
                {
                    double algos = Utils.MicroalgosToAlgos((ulong)_trans.Payment.Amount);
                    if (_trans.From == this.accountAddress)
                        return "-" + algos.ToString();
                    else if(_trans.Payment.To == accountAddress)                    
                        return "+" + algos.ToString();                    
                }
                return "-";
            } }
        public TransInfo(Algorand.Algod.Client.Model.Transaction trans, string accountAddress)
        {
            this._trans = trans;
            this.accountAddress = accountAddress;
        }
    }
}
