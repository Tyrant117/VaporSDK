using System.Collections.Generic;
using UnityEngine;
using Vapor.Graphs;

namespace Vapor.Graphs
{
    [NodeName("Return")]
    public class FunctionReturnNodeModel : NodeModel
    {
        public override bool HasInPort => true;
    }
}
