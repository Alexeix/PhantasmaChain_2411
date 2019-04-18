﻿using System;
using System.Collections.Generic;
using Phantasma.Core.Utils;
using Phantasma.Cryptography;
using Phantasma.Cryptography.ECC;
using Phantasma.Numerics;

namespace Phantasma.Pay.Chains
{
    public class BitcoinWallet: CryptoWallet
    {
        private List<Unspent> _unspents = new List<Unspent>();

        public struct Unspent
        {
            public BigInteger index;
            public decimal amount;
            public string script;
        }

        public BitcoinWallet(KeyPair keys) : base(keys)
        {
        }

        public override WalletKind Kind => WalletKind.Bitcoin;

        public override void MakePayment(string symbol, decimal amount, string targetAddress, Action<bool> callback)
        {
            throw new NotImplementedException();
        }

        public override void SyncBalances(Action<bool> callback)
        {
            _balances.Clear();
            _unspents.Clear();

            var url = "https://blockchain.info/rawaddr/" + this.Address;
            JSONRequest(url, (root) =>
            {
                if (root == null)
                {
                    callback(false);
                    return;
                }

                decimal btcBalance = 0;

                var txNode = root["txs"];
                foreach (var node in txNode.Children)
                {
                    var outputsNode = node["out"];
                    foreach (var outputNode in outputsNode.Children)
                    {
                        var addr = outputNode.GetString("addr");
                        if (addr != this.Address)
                        {
                            continue;
                        }

                        bool spent = outputNode.GetBool("spent");
                        if (spent == false)
                        {
                            var unspent = new Unspent();
                            unspent.index = BigInteger.Parse(outputNode.GetString("tx_index"));
                            unspent.script = outputNode.GetString("script");
                            var temp = BigInteger.Parse(outputNode.GetString("value"));
                            unspent.amount = UnitConversion.ToDecimal(temp, 8);
                            btcBalance += unspent.amount;
                            _unspents.Add(unspent);
                        }
                    }
                }

                _balances.Add(new WalletBalance("BTC", btcBalance));

                callback(true);
            });            
        }

        protected override string DeriveAddress(KeyPair keys)
        {
            ECPoint pKey = ECCurve.Secp256k1.G * keys.PrivateKey;

            var publicKey = pKey.EncodePoint(true);

            var bytes = ByteArrayUtils.ConcatBytes(new byte[] { 0 }, publicKey.SHA256().RIPEMD160());

            return bytes.Base58CheckEncode();
        }

        public override IEnumerable<CryptoCurrencyInfo> GetCryptoCurrencyInfos()
        {
            yield return new CryptoCurrencyInfo("BTC", "Bitcoin", 8, WalletKind.Bitcoin);
            yield break;
        }
    }
}
