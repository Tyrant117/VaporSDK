using UnityEngine;

namespace Vapor.Blueprints
{
    public enum VariableScopeType
    {
        Block,
        Method,
        Class,
    }

    public enum VariableAccessType
    {
        Get,
        Set,
    }

    public enum VariableAccessModifier
    {
        Public,
        Protected,
        Private,
    }

    public enum NodeType
    {
        Entry,
        Method,
        MemberAccess,
        Return,
        
        // Flow
        Branch,
        Switch,
        Sequence,
        
        // Iterators
        For,
        ForEach,
        While,
        Break,
        Continue,
        
        // Type Management
        Conversion,
        Cast,
        
        // Utility
        Redirect,
        Inline,
    }
    
    public enum PinDirection
    {
        In,
        Out
    }
}
