using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using Vapor;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public abstract class BlueprintSyntaxNodeBase
    {
        protected readonly BlueprintDesignNode MyNode;

        protected BlueprintSyntaxNodeBase(BlueprintDesignNode node)
        {
            MyNode = node;
        }
        
        public abstract BlockSyntax AddStatementAndContinue(BlockSyntax block);

        public virtual BlockSyntax GetStatementForPin(BlueprintWireReference wire, BlockSyntax block)
        {
            return block;
        }

        public string FormatWithUid(string prefix) => MyNode.FormatWithUuid(prefix);

        public string FormatWithGuid(string prefix, string guid) => $"{prefix}_{guid.GetStableHashU32()}";

        public static BlueprintSyntaxNodeBase ConvertToSyntaxNode(BlueprintDesignNode node)
        {
            switch (node.NodeType)
            {
                case BranchNodeType:
                    return new BranchSyntaxNode(node);
                case ConverterNodeType:
                    break;
                case EntryNodeType:
                    return new EntrySyntaxNode(node);
                case FieldGetterNodeType:
                    break;
                case FieldSetterNodeType:
                    break;
                case ForEachNodeType:
                    break;
                case ForNodeType:
                    break;
                case GraphNodeType:
                    break;
                case MakeSerializableNodeType:
                    break;
                case MethodNodeType:
                    return new MethodSyntaxNode(node);
                case RerouteNodeType:
                    break;
                case ReturnNodeType:
                    return new ReturnSyntaxNode(node);
                case SequenceNodeType:
                    break;
                case SwitchNodeType:
                    break;
                case TemporaryDataGetterNodeType:
                    break;
                case TemporaryDataSetterNodeType:
                    return new SetLocalVariableSyntaxNode(node);
                case WhileNodeType:
                    break;
            }

            return null;
        }
    }

    public class EntrySyntaxNode : BlueprintSyntaxNodeBase
    {
        public EntrySyntaxNode(BlueprintDesignNode node) : base(node)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            var nextNodeGuid = MyNode.OutputWires.First(w => w.IsExecuteWire).RightSidePin.NodeGuid;
            if (!BlueprintScriptWriter.NodeMap.TryGetValue(nextNodeGuid, out var nextNode))
            {
                Debug.LogError($"Invalid Node Guid: {nextNodeGuid}");
                return block;
            }

            // Create all Local Variables First when entered
            foreach (var tmp in MyNode.Graph.TemporaryVariables)
            {
                // Use Default Value
                var defaultValue = BlueprintScriptWriter.GetDefaultValueSyntax(tmp.Type);

                var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(tmp.Type))
                        .AddVariables(SyntaxFactory.VariableDeclarator(tmp.Name)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
                );
                block = block.AddStatements(variableDeclaration);
            }
            
            block = nextNode.AddStatementAndContinue(block);
            return block;
        }

        public override BlockSyntax GetStatementForPin(BlueprintWireReference wire, BlockSyntax block)
        {
            if (MyNode.OutPorts.ContainsKey(wire.LeftSidePin.PinName))
            {
                block = block.AddStatements(SyntaxFactory.ParseStatement($"var {wire.RightSidePin.PinName}_{wire.RightSidePin.NodeGuid.GetStableHashU32()} = {wire.LeftSidePin.PinName};"));
            }
            return block;
        }
    }

    public class ReturnSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ReturnSyntaxNode(BlueprintDesignNode node) : base(node)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (MyNode.InPorts.Count == 1)
            {
                block = block.AddStatements(SyntaxFactory.ReturnStatement());
            }
            else
            {
                foreach (var pin in MyNode.InPorts.Values)
                {
                    if (pin.IsExecutePin)
                    {
                        continue;
                    }

                    var wire = MyNode.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == pin.PortName);
                    if (wire.IsValid())
                    {
                        block = GetStatementForPin(wire, block);
                    }
                    else
                    {
                        // Use Default Value
                        var defaultValue = pin.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(pin.Type) : BlueprintScriptWriter.GetExpressionForObjectInitializer(pin.InlineValue.Get());

                        var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
                                .AddVariables(SyntaxFactory.VariableDeclarator(MyNode.FormatWithUuid(pin.PortName))
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
                        );
                        block = block.AddStatements(variableDeclaration);
                    }
                }
                
                block = block.AddStatements(SyntaxFactory.ReturnStatement(
                    SyntaxFactory.IdentifierName(MyNode.FormatWithUuid("Return"))
                ));
            }
            return block;
        }

        public override BlockSyntax GetStatementForPin(BlueprintWireReference wire, BlockSyntax block)
        {
            if (MyNode.InPorts.TryGetValue(wire.RightSidePin.PinName, out var pin))
            {
                if (!BlueprintScriptWriter.NodeMap.TryGetValue(wire.LeftSidePin.NodeGuid, out var nextNode))
                {
                    // Use Default Value
                    var defaultValue = pin.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(pin.Type) :BlueprintScriptWriter.GetExpressionForObjectInitializer(pin.InlineValue.Get());

                    var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
                            .AddVariables(SyntaxFactory.VariableDeclarator(MyNode.FormatWithUuid(pin.PortName))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
                    );
                    block = block.AddStatements(variableDeclaration);
                }
                else
                {
                    block = nextNode.GetStatementForPin(wire, block);

                    // Create a variable declaration: <ReturnType> __returnValue = <defaultValue>;
                    var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
                            .AddVariables(SyntaxFactory.VariableDeclarator(MyNode.FormatWithUuid(pin.PortName))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(nextNode.FormatWithUid(wire.LeftSidePin.PinName))))) // Assign default value
                    );
                    block = block.AddStatements(variableDeclaration);
                }
            }
            return block;
        }
    }
    
    public class MethodSyntaxNode : BlueprintSyntaxNodeBase
    {
        private readonly MethodInfo _methodInfo;
        
        public MethodSyntaxNode(BlueprintDesignNode node) : base(node)
        {
            node.TryGetProperty(BlueprintDesignNode.K_METHOD_INFO, out _methodInfo);
        }
        
        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            var wire = MyNode.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.EXECUTE_OUT);
            if (wire.IsValid())
            {
                var @params = MyNode.InPorts.Values.ToList().FindAll(w => !w.IsExecutePin);
                var argNames = @params.Select(w => w.PortName).Where(n => n != PinNames.OWNER).ToArray();
                for (int i = 0; i < argNames.Length; i++)
                {
                    argNames[i] = MyNode.FormatWithUuid(argNames[i]);
                }
                var nextNodeGuid = wire.RightSidePin.NodeGuid;
                string invokingVarName = null;
                if (MyNode.InPorts.ContainsKey(PinNames.OWNER))
                {
                    invokingVarName = $"{PinNames.OWNER}_{MyNode.Uuid}";
                }

                block = BlueprintScriptWriter.AddMethodInfoToBody(string.Empty, argNames, invokingVarName, _methodInfo, block);
                if (!BlueprintScriptWriter.NodeMap.TryGetValue(nextNodeGuid, out var nextNode))
                {
                    Debug.LogError($"Invalid Node Guid: {nextNodeGuid}");
                    return block;
                }
                block = nextNode.AddStatementAndContinue(block);
            }
            return block;
        }

        public override BlockSyntax GetStatementForPin(BlueprintWireReference wire, BlockSyntax block)
        {
            var executeWire = MyNode.OutputWires.FirstOrDefault(w => w.IsExecuteWire);
            if (!executeWire.IsValid() && MyNode.OutPorts.TryGetValue(wire.LeftSidePin.PinName, out var pin))
            {
                foreach (var p in MyNode.InPorts.Values)
                {
                    if (p.IsExecutePin)
                    {
                        continue;
                    }
                    var w = MyNode.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == p.PortName);
                    if (w.IsValid())
                    {
                        if (!BlueprintScriptWriter.NodeMap.TryGetValue(w.LeftSidePin.NodeGuid, out var node))
                        {
                            Debug.LogError($"Invalid Node Guid: {w.LeftSidePin.NodeGuid}");
                            continue;
                        }

                        block = node.GetStatementForPin(w, block);
                    }
                    else
                    {
                        // Use Default Value
                        var defaultValue = p.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(p.Type) : BlueprintScriptWriter.GetExpressionForObjectInitializer(p.InlineValue.Get());

                        var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(p.Type))
                                .AddVariables(SyntaxFactory.VariableDeclarator(MyNode.FormatWithUuid(p.PortName))
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
                        );
                        block = block.AddStatements(variableDeclaration);
                    }
                }
                
                var @params = MyNode.InPorts.Values.ToList().FindAll(w => !w.IsExecutePin);
                var argNames = @params.Select(w => w.PortName).Where(n => n != PinNames.OWNER).ToArray();
                for (int i = 0; i < argNames.Length; i++)
                {
                    argNames[i] = MyNode.FormatWithUuid(argNames[i]);
                }
                string invokingVarName = null;
                if (MyNode.InPorts.ContainsKey(PinNames.OWNER))
                {
                    invokingVarName = $"{PinNames.OWNER}_{MyNode.Uuid}";
                }
                block = BlueprintScriptWriter.AddMethodInfoToBody(MyNode.FormatWithUuid(wire.LeftSidePin.PinName), argNames, invokingVarName, _methodInfo, block);
            }

            return block;
        }
    }

    public class BranchSyntaxNode : BlueprintSyntaxNodeBase
    {
        public BranchSyntaxNode(BlueprintDesignNode node) : base(node)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            // Evaluate True Expression
            var conditionBlock = SyntaxFactory.Block();
            
            var boolVariableName = "conditionMet";
            var boolVariableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)))
                    .AddVariables(SyntaxFactory.VariableDeclarator(boolVariableName)));
            
            // Create an if-else statement
            var ifBlock = SyntaxFactory.Block();
            var elseBlock = SyntaxFactory.Block();
            
            // Follow Path
            
            IfStatementSyntax ifElseStatement = SyntaxFactory
                .IfStatement(SyntaxFactory.IdentifierName(boolVariableName), ifBlock)
                .WithElse(SyntaxFactory.ElseClause(elseBlock));

            // Add the if-else statement to the existing block and return
            block = block.AddStatements(boolVariableDeclaration, ifElseStatement);
            return block;
        }
    }

    public class SetLocalVariableSyntaxNode : BlueprintSyntaxNodeBase
    {
        public SetLocalVariableSyntaxNode(BlueprintDesignNode node) : base(node)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            MyNode.TryGetProperty<string>(BlueprintDesignNode.VARIABLE_NAME, out var tempFieldName);
            
            var wire = MyNode.InputWires.FirstOrDefault(w => !w.IsExecuteWire);
            if (wire.IsValid())
            {
                if (!BlueprintScriptWriter.NodeMap.TryGetValue(wire.LeftSidePin.NodeGuid, out var node))
                {
                    Debug.LogError($"Invalid Node Guid: {wire.LeftSidePin.NodeGuid}");
                    return block;
                }
                block = node.GetStatementForPin(wire, block);
                
                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(tempFieldName), // Left-hand side
                    SyntaxFactory.IdentifierName(node.FormatWithUid(wire.LeftSidePin.PinName)) // Right-hand side
                );
                var statement = SyntaxFactory.ExpressionStatement(assignmentExpression);
                block = block.AddStatements(statement);
            }
            
            var nextNodeGuid = MyNode.OutputWires.First(w => w.IsExecuteWire).RightSidePin.NodeGuid;
            if (!BlueprintScriptWriter.NodeMap.TryGetValue(nextNodeGuid, out var nextNode))
            {
                Debug.LogError($"Invalid Node Guid: {nextNodeGuid}");
                return block;
            }
            
            block = nextNode.AddStatementAndContinue(block);
            return block;
        }
    }
}
