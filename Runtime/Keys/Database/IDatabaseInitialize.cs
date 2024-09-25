using UnityEngine;

namespace Vapor
{
    public interface IDatabaseInitialize
    {
        /// <summary>
        /// This method is called once when this item is loaded into the database at runtime.
        /// </summary>
        void InitializedInDatabase();
    }
}
