using System.Collections.Generic;
using UnityEngine;
using Vapor.VisualScripting;

namespace Vapor.VisualScripting
{
    [NodeName("Return")]
    public class FunctionReturnNodeModel : NodeModel
    {
        public override bool HasInPort => true;
    }
}
