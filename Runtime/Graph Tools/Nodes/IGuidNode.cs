using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.GraphTools
{
    public interface IGuidNode
    {
        string CreateGuid()
        {
            return Guid.NewGuid().ToString();
        }

        void SetGuid(string guid);

        string GetGuid();
    }
}
