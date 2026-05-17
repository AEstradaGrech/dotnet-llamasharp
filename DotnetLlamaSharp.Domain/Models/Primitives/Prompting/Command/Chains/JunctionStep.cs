using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetLlamaSharp.Domain.Models.Primitives.Prompting.Command.Chains
{
    public class JunctionStep : SingleThrowStep
    {
        public override bool CanBeForged(IChaineable previous)
        {
            throw new NotImplementedException();
        }

        public override Task<IChaineable> Forge(IChaineable previous)
        {
            throw new NotImplementedException();
        }
    }
}
