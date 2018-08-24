﻿using System;
using System.IO;
using Phantasma.VM.Contracts;
using Phantasma.Cryptography;
using Phantasma.Utils;
using Phantasma.VM;
using Phantasma.VM.Types;
using Phantasma.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Blockchain
{
    public sealed class Transaction: ITransaction
    {
        public BigInteger Fee { get; }
        public BigInteger Index { get; }
        public byte[] Script { get; }

        public Signature[] Signatures { get; private set; }
        public Hash Hash { get; private set; }

        public static Transaction Unserialize(BinaryReader reader)
        {
            var script = reader.ReadByteArray();
            var fee = reader.ReadBigInteger();
            var txOrder = reader.ReadBigInteger();
            var signatureCount = (int)reader.ReadVarInt();

            var signatures = new Signature[signatureCount];
            for (int i=0; i<signatureCount; i++)
            {
                signatures[i] = reader.ReadSignature();
            }

            return new Transaction(script, fee, txOrder, signatures);
        }

        private void Serialize(BinaryWriter writer, bool withSignature)
        {
            writer.WriteByteArray(this.Script);
            writer.WriteBigInteger(this.Fee);
            writer.WriteBigInteger(this.Index);

            if (withSignature)
            {
                writer.WriteVarInt(Signatures.Length);
                foreach (var signature in this.Signatures)
                {
                    writer.WriteSignature(signature);
                }
            }
        }

        // TODO should run the script and return true if sucess or false if exception
        private bool Validate(Chain chain, out BigInteger fee)
        {
            fee = 0;
            return true;
        }

        internal bool Execute(Chain chain, Action<Event> notify)
        {
            var vm = new RuntimeVM(chain, this);

            var state = vm.Execute();

            if (state != ExecutionState.Halt)
            {
                return false;
            }

            var cost = vm.gas;

            // fee distribution TODO
//            if (chain.NativeTokenAddress != null && cost > 0)
            {
                //chain.TransferToken(this.PublicKey, chain.DistributionPubKey, cost);
            }

            // TODO take storage changes from vm execution and apply to global state
            //this.Apply(chain, notify);

            return true;
        }

        public Transaction(byte[] script, BigInteger fee, BigInteger txOrder, IEnumerable<Signature> signatures = null)
        {
            this.Script = script;
            this.Fee = fee;
            this.Index = txOrder;

            this.Signatures = signatures != null && signatures.Any() ? signatures.ToArray() : new Signature[0];

            this.UpdateHash();
        }

        public byte[] ToArray(bool withSignature)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    Serialize(writer, withSignature);
                }

                return stream.ToArray();
            }
        }

        public bool IsSigned => Signatures != null && Signatures.Length > 0;

        public bool Sign(KeyPair owner)
        {
            if (IsSigned)
            {
                return false;
            }

            if (owner == null)
            {
                return false;
            }

            var msg = this.ToArray(false);
            this.Signatures = new Signature[] { owner.Sign(msg) };

            return true;
        }

        public bool IsValid(Chain chain)
        {
            // TODO unsigned tx should be supported too
            /* if (!IsSigned) 
             {
                 return false;
             }

             var data = ToArray(false);
             if (!this.Signature.Verify(data, this.SourceAddress))
             {
                 return false;
             }*/

            BigInteger cost;
            var validation = Validate(chain, out cost);
            if (!validation)
            {
                return false;
            }
           
            /*if (chain.NativeTokenAddress != null)
            {
                if (this.Fee < cost)
                {
                    return false;
                }

                var balance = chain.GetTokenBalance(chain.NativeTokenAddress, this.SourceAddress);

                if (balance < this.Fee)
                {
                    return false;
                }
            }*/

            return true;
        }

        private void UpdateHash()
        {
            var data = this.ToArray(false);
            var hash = CryptoUtils.Sha256(data);
            this.Hash = new Hash(hash);
        }

    }
}