﻿using System.Collections.Generic;
using Phantasma.Cryptography;
using Phantasma.Numerics;
using Phantasma.Core.Types;
using Phantasma.Blockchain.Tokens;
using System.Linq;

namespace Phantasma.Blockchain.Consensus
{
    public class ProofOfWork
    {
        public static Block MineBlock(Chain chain, Address minerAddress, IEnumerable<Transaction> txs, byte[] extraContent = null)
        {
            var timestamp = Timestamp.Now;

            var hashes = txs.Select(tx => tx.Hash);
            var block = new Block(chain.LastBlock.Height + 1, chain.Address, minerAddress, timestamp, hashes, chain.LastBlock.Hash, extraContent);

            var blockDifficulty = Block.InitialDifficulty; // TODO change this later

            LargeInteger target = 0;
            for (int i = 0; i <= blockDifficulty; i++)
            {
                LargeInteger k = 1;
                k <<= i;
                target += k;
            }

            do
            {
                LargeInteger n = new LargeInteger(block.Hash.ToByteArray());
                if (n < target)
                {
                    break;
                }

                block.UpdateHash(block.Nonce + 1);
            } while (true);

            return block;
        }
    }
}
