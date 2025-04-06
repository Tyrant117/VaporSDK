using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using Vapor;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public abstract class BlueprintSyntaxNodeBase
    {
        protected internal readonly NodeModelBase MyNodeController;

        protected bool IsEvaluated { get; set; }
        protected int Counter { get; set; }

        protected BlueprintSyntaxNodeBase(NodeModelBase nodeController)
        {
            MyNodeController = nodeController;
        }

        public virtual BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            var idx = MyNodeController.OutputWires.FindIndex(w => w.IsExecuteWire);
            if (idx == -1)
            {
                return block;
            }

            var nextNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.IsExecuteWire);
            if (!nextNodeWire.IsValid() || !BlueprintScriptWriter.NodeMap.TryGetValue(nextNodeWire.RightSidePin.NodeGuid, out var nextNode))
            {
                Debug.LogError($"Invalid Node Guid: {nextNodeWire.RightSidePin.PinName}");
                return block;
            }

            block = nextNode.AddStatementAndContinue(block);
            return block;
        }

        protected BlockSyntax GetInputPinStatements(BlockSyntax block)
        {
            foreach (var pin in MyNodeController.InputPins.Values)
            {
                if (pin.IsExecutePin)
                {
                    continue;
                }

                if (pin.PortName == PinNames.IGNORE)
                {
                    continue;
                }

                var wire = MyNodeController.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == pin.PortName);
                if (wire.IsValid())
                {
                    if (!BlueprintScriptWriter.NodeMap.TryGetValue(wire.LeftSidePin.NodeGuid, out var pinNode))
                    {
                        Debug.LogError($"Invalid Node Guid: {wire.LeftSidePin.NodeGuid}");
                        return block;
                    }
                    block = pinNode.GetStatementForPin(wire, block);
                }
                else
                {
                    // Use Default Value
                    var defaultValue = pin.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(pin.Type) : BlueprintScriptWriter.GetExpressionForObjectInitializer(pin.InlineValue.Get());

                    var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
                            .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithUid(pin.PortName))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
                    );
                    block = block.AddStatements(variableDeclaration);
                }
            }
            return block;
        }

        // public virtual BlockSyntax CreateActionStatement(BlockSyntax block)
        // {
        //     return block;
        // }

        protected virtual BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            return block;
        }

        private BlockSyntax GetStatementForPin(BlueprintWireReference wire, BlockSyntax block)
        {
            // If the node hasn't been evaluated meaning a variable has not been set for it yet.
            if (!IsEvaluated)
            {
                Counter++;
                block = GetInputPinStatements(block);
                block = SetOutputPinStatements(block);
            }

            var pinType = MyNodeController.OutputPins[wire.LeftSidePin.PinName].Type;
            var rightCounter = BlueprintScriptWriter.NodeMap[wire.RightSidePin.NodeGuid].Counter;

            // Then create a variable {Type RightNameIn = LeftNameOut}
            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pinType))
                    .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithGuid(wire.RightSidePin.PinName, wire.RightSidePin.NodeGuid, rightCounter))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(FormatWithGuid(wire.LeftSidePin.PinName, wire.LeftSidePin.NodeGuid, Counter))))) // Assign default value
            );
            block = block.AddStatements(variableDeclaration);

            return block;
        }

        protected string FormatWithUid(string prefix) => $"{MyNodeController.FormatWithUuid(prefix)}_{Counter}";

        private static string FormatWithGuid(string prefix, string guid, int count) => $"{prefix}_{guid.GetStableHashU32()}_{count}";

        public static BlueprintSyntaxNodeBase ConvertToSyntaxNode(NodeModelBase nodeController)
        {
            return nodeController.NodeType switch
            {
                NodeType.Entry => new EntrySyntaxNode(nodeController),
                NodeType.Method => new MethodSyntaxNode((MethodNodeModel)nodeController),
                NodeType.MemberAccess => new MemberAccessSyntaxNode((MemberNodeModel)nodeController),
                NodeType.Return => new ReturnSyntaxNode(nodeController),
                NodeType.Branch => new BranchSyntaxNode(nodeController),
                NodeType.Switch => new SwitchSyntaxNode(nodeController),
                NodeType.Sequence => new SequenceSyntaxNode(nodeController),
                NodeType.For => new ForSyntaxNode(nodeController),
                NodeType.ForEach => new ForEachSyntaxNode(nodeController),
                NodeType.While => new WhileSyntaxNode(nodeController),
                NodeType.Break => new BreakSyntaxNode(nodeController),
                NodeType.Continue => new ContinueSyntaxNode(nodeController),
                NodeType.Conversion => new ConversionSyntaxNode(nodeController),
                NodeType.Cast => new CastSyntaxNode(nodeController),
                NodeType.Redirect => new RedirectSyntaxNode(nodeController),
                NodeType.Inline => new ConstructorSyntaxNode(nodeController),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public class EntrySyntaxNode : BlueprintSyntaxNodeBase
    {
        public EntrySyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            // Create All Input Args
            foreach (var arg in MyNodeController.OutputPins.Values)
            {
                if (arg.IsExecutePin)
                {
                    continue;
                }
                
                var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(arg.Type))
                        .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithUid(arg.PortName))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(arg.PortName)))) // Assign generated value
                );
                block = block.AddStatements(variableDeclaration);
            }
            
            // Create all Local Variables First when entered
            foreach (var tmp in MyNodeController.Method.Variables)
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

            return block;
        }
    }
    
    public class MethodSyntaxNode : BlueprintSyntaxNodeBase
    {
        private readonly MethodNodeModel _model;
        private readonly MethodInfo _methodInfo;
        
        public MethodSyntaxNode(MethodNodeModel nodeController) : base(nodeController)
        {
            _model = nodeController;
            _methodInfo = nodeController.MethodInfo;
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }
            
            block = GetInputPinStatements(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            var @params = MyNodeController.InputPins.Values.ToList().FindAll(w => !w.IsExecutePin);
            var outParams = MyNodeController.OutputPins.Values.ToList().FindAll(w => !w.IsExecutePin);
            var argNames = @params.Select(w => w.PortName).Where(n => n != PinNames.OWNER).ToArray();
            var outArgNames = outParams.Select(w => w.PortName).Where(n => n != PinNames.RETURN).ToArray();
            var paramInfos = _methodInfo.GetParameters();
            
            var parameters = (from t in argNames let pi = paramInfos.First(p => p.Name == t) select (pi, FormatWithUid(t))).ToList();
            parameters.AddRange(from t in outArgNames let pi = paramInfos.First(p => p.Name == t) select (pi, FormatWithUid(t)));


            string invokingVarName = null;
            if (MyNodeController.InputPins.ContainsKey(PinNames.OWNER))
            {
                invokingVarName = FormatWithUid(PinNames.OWNER);
            }

            var returnPinName = string.Empty;
            Type returnPinType = null;
            if (MyNodeController.OutputPins.TryGetValue(PinNames.RETURN, out var pin))
            {
                returnPinName = FormatWithUid(PinNames.RETURN);
                returnPinType = pin.Type;
            }
            
            var genericTypes = new List<Type>();
            if (_methodInfo.IsGenericMethod)
            {
                var genArgs = _methodInfo.GetGenericArguments();
                foreach (var arg in genArgs)
                {
                    if (!_model.GenericArgumentPortMap.TryGetValue(arg, out var p))
                    {
                        continue;
                    }

                    genericTypes.Add(p.Type);
                }
            }
            
            // var genericTypes = (from p in MyNodeController.GenericArgumentPortMap select p.Value.Type).ToList();

            block = BlueprintScriptWriter.AddMethodInfoToBody((returnPinType, returnPinName), parameters, genericTypes, invokingVarName, _methodInfo, block);

            var idx = MyNodeController.OutputWires.FindIndex(w => w.IsExecuteWire);
            if (idx == -1)
            {
                return block;
            }

            var nextNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.IsExecuteWire);
            if (!nextNodeWire.IsValid() || !BlueprintScriptWriter.NodeMap.TryGetValue(nextNodeWire.RightSidePin.NodeGuid, out var nextNode))
            {
                Debug.LogError($"Invalid Node Guid: {nextNodeWire.RightSidePin.PinName}");
                return block;
            }

            block = nextNode.AddStatementAndContinue(block);
            return block;
        }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            if (!_model.IsPure)
            {
                return block;
            }

            var @params = MyNodeController.InputPins.Values.ToList().FindAll(w => !w.IsExecutePin);
            var outParams = MyNodeController.OutputPins.Values.ToList().FindAll(w => !w.IsExecutePin);
            var argNames = @params.Select(w => w.PortName).Where(n => n != PinNames.OWNER).ToArray();
            var outArgNames = outParams.Select(w => w.PortName).Where(n => n != PinNames.RETURN).ToArray();
            var paramInfos = _methodInfo.GetParameters();
            
            var parameters = (from t in argNames let pi = paramInfos.First(p => p.Name == t) select (pi, FormatWithUid(t))).ToList();
            parameters.AddRange(from t in outArgNames let pi = paramInfos.First(p => p.Name == t) select (pi, FormatWithUid(t)));


            string invokingVarName = null;
            if (MyNodeController.InputPins.ContainsKey(PinNames.OWNER))
            {
                invokingVarName = $"{PinNames.OWNER}_{MyNodeController.Uuid}";
            }

            var returnPinName = string.Empty;
            Type returnPinType = null;
            if (MyNodeController.OutputPins.TryGetValue(PinNames.RETURN, out var pin))
            {
                returnPinName = FormatWithUid(PinNames.RETURN);
                returnPinType = pin.Type;
            }
            
            var genericTypes = (from p in _model.GenericArgumentPortMap select p.Value.Type).ToList();

            block = BlueprintScriptWriter.AddMethodInfoToBody((returnPinType, returnPinName), parameters, genericTypes, invokingVarName, _methodInfo, block);
            return block;
        }
    }

    public class MemberAccessSyntaxNode : BlueprintSyntaxNodeBase
    {
        private readonly MemberNodeModel _model;

        public MemberAccessSyntaxNode(MemberNodeModel nodeController) : base(nodeController)
        {
            _model = nodeController;
        }

        // public override BlockSyntax CreateActionStatement(BlockSyntax block)
        // {
        //     var model = MyNodeController.ModelAs<MemberNodeModel>();
        //     if (model.FieldInfo == null)
        //     {
        //         if (model.VariableAccess == VariableAccessType.Set)
        //         {
        //             var assignmentExpression = SyntaxFactory.AssignmentExpression(
        //                 SyntaxKind.SimpleAssignmentExpression,
        //                 SyntaxFactory.IdentifierName(model.VariableName), // Left-hand side
        //                 SyntaxFactory.IdentifierName(FormatWithUid(PinNames.SET_IN)) // Right-hand side
        //             );
        //             var statement = SyntaxFactory.ExpressionStatement(assignmentExpression);
        //             block = block.AddStatements(statement);
        //         }
        //     }
        //     else
        //     {
        //         if (model.VariableAccess == VariableAccessType.Set)
        //         {
        //            block = BlueprintScriptWriter.AddSetFieldToBody(model.FieldInfo,  FormatWithUid(PinNames.SET_IN), FormatWithUid(PinNames.OWNER), block);
        //         }
        //     }
        //     
        //     var pinType = MyNodeController.OutputPins[PinNames.GET_OUT].Type;
        //     // Then create a variable {Type RightNameIn = LeftNameOut}
        //     var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
        //         SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pinType))
        //             .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithUid(PinNames.GET_OUT)))
        //     );
        //     block = block.AddStatements(variableDeclaration);
        //     
        //     return block;
        // }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            if (_model.FieldInfo == null)
            {
                if (_model.VariableAccess == VariableAccessType.Set)
                {
                    var assignmentExpression = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(_model.VariableName), // Left-hand side
                        SyntaxFactory.IdentifierName(FormatWithUid(PinNames.SET_IN)) // Right-hand side
                    );
                    var statement = SyntaxFactory.ExpressionStatement(assignmentExpression);
                    block = block.AddStatements(statement);
                }
            }
            else
            {
                if (_model.VariableAccess == VariableAccessType.Set)
                {
                    block = BlueprintScriptWriter.AddSetFieldToBody(_model.FieldInfo,  FormatWithUid(PinNames.SET_IN), FormatWithUid(PinNames.OWNER), block);
                }
            }
            
            var pinType = MyNodeController.OutputPins[PinNames.GET_OUT].Type;
            // Then create a variable {Type RightNameIn = LeftNameOut}
            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pinType))
                    .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithUid(PinNames.GET_OUT)))
            );
            block = block.AddStatements(variableDeclaration);
            
            if (_model.FieldInfo == null)
            {
                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(FormatWithUid(PinNames.GET_OUT)), // Left-hand side
                    SyntaxFactory.IdentifierName(_model.VariableName) // Right-hand side
                );
                var statement = SyntaxFactory.ExpressionStatement(assignmentExpression);
                block = block.AddStatements(statement);
            }
            else
            {
                block = BlueprintScriptWriter.AddGetFieldToBody(_model.FieldInfo,  FormatWithUid(PinNames.GET_OUT), FormatWithUid(PinNames.OWNER), block);
            }
            
            return block;
        }
    }

    public class ReturnSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ReturnSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            if (MyNodeController.InputPins.Count == 1)
            {
                block = block.AddStatements(SyntaxFactory.ReturnStatement());
            }
            else
            {
                block = block.AddStatements(SyntaxFactory.ReturnStatement(
                    SyntaxFactory.IdentifierName(FormatWithUid(PinNames.RETURN))
                ));
            }
            return block;
        }

        // public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        // {
        //     if (MyNodeController.InputPins.Count == 1)
        //     {
        //         block = block.AddStatements(SyntaxFactory.ReturnStatement());
        //     }
        //     else
        //     {
        //         foreach (var pin in MyNodeController.InputPins.Values)
        //         {
        //             if (pin.IsExecutePin)
        //             {
        //                 continue;
        //             }
        //
        //             var wire = MyNodeController.Model.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == pin.PortName);
        //             if (wire.IsValid())
        //             {
        //                 block = GetStatementForPin(wire, block);
        //             }
        //             else
        //             {
        //                 // Use Default Value
        //                 var defaultValue = pin.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(pin.Type) : BlueprintScriptWriter.GetExpressionForObjectInitializer(pin.InlineValue.Get());
        //
        //                 var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
        //                     SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
        //                         .AddVariables(SyntaxFactory.VariableDeclarator(MyNodeController.FormatWithUuid(pin.PortName))
        //                             .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
        //                 );
        //                 block = block.AddStatements(variableDeclaration);
        //             }
        //         }
        //         
        //         block = block.AddStatements(SyntaxFactory.ReturnStatement(
        //             SyntaxFactory.IdentifierName(MyNodeController.FormatWithUuid("Return"))
        //         ));
        //     }
        //     return block;
        // }

        // public override BlockSyntax GetStatementForPin(BlueprintWireReference wire, BlockSyntax block)
        // {
        //     if (MyNodeController.InputPins.TryGetValue(wire.RightSidePin.PinName, out var pin))
        //     {
        //         if (!BlueprintScriptWriter.NodeMap.TryGetValue(wire.LeftSidePin.NodeGuid, out var nextNode))
        //         {
        //             // Use Default Value
        //             var defaultValue = pin.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(pin.Type) :BlueprintScriptWriter.GetExpressionForObjectInitializer(pin.InlineValue.Get());
        //
        //             var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
        //                 SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
        //                     .AddVariables(SyntaxFactory.VariableDeclarator(MyNodeController.FormatWithUuid(pin.PortName))
        //                         .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value
        //             );
        //             block = block.AddStatements(variableDeclaration);
        //         }
        //         else
        //         {
        //             block = nextNode.GetStatementForPin(wire, block);
        //
        //             // Create a variable declaration: <ReturnType> __returnValue = <defaultValue>;
        //             var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
        //                 SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
        //                     .AddVariables(SyntaxFactory.VariableDeclarator(MyNodeController.FormatWithUuid(pin.PortName))
        //                         .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(nextNode.FormatWithUid(wire.LeftSidePin.PinName))))) // Assign default value
        //             );
        //             block = block.AddStatements(variableDeclaration);
        //         }
        //     }
        //     return block;
        // }
    }
    
    public class BranchSyntaxNode : BlueprintSyntaxNodeBase
    {
        public BranchSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;
            
            // Create an if-else statement
            var ifBlock = SyntaxFactory.Block();
            var elseBlock = SyntaxFactory.Block();

            var ifNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.TRUE_OUT);
            if (ifNodeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(ifNodeWire.RightSidePin.NodeGuid, out var ifNode))
            {
                ifBlock = ifNode.AddStatementAndContinue(ifBlock);
            }

            var elseNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.FALSE_OUT);
            if (elseNodeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(elseNodeWire.RightSidePin.NodeGuid, out var elseNode))
            {
                elseBlock = elseNode.AddStatementAndContinue(elseBlock);
            }

            IfStatementSyntax ifElseStatement = SyntaxFactory
                .IfStatement(SyntaxFactory.IdentifierName(FormatWithUid(PinNames.VALUE_IN)), ifBlock)
                .WithElse(SyntaxFactory.ElseClause(elseBlock));
            
            // Add the if-else statement to the existing block and return
            block = block.AddStatements(ifElseStatement);
            return block;
        }
    }

    public class SwitchSyntaxNode : BlueprintSyntaxNodeBase
    {
        public SwitchSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            // Create a switch statement
            var switchType = MyNodeController.InputPins[PinNames.VALUE_IN].Type;
            if(switchType == typeof(Enum))
            {
                var typeWire = MyNodeController.InputWires.FirstOrDefault(w => w.RightSidePin.PinName == PinNames.VALUE_IN);
                if (typeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(typeWire.LeftSidePin.NodeGuid, out var typeNode))
                {
                    switchType = typeNode.MyNodeController.OutputPins[typeWire.LeftSidePin.PinName].Type;
                }
            }
            
            var switchVariableName = FormatWithUid(MyNodeController.InputPins[PinNames.VALUE_IN].PortName);

            var cases = new List<SwitchSectionSyntax>();
            BlockSyntax defaultCase = null;
            foreach (var outputWire in MyNodeController.OutputWires)
            {
                if (!BlueprintScriptWriter.NodeMap.TryGetValue(outputWire.RightSidePin.NodeGuid, out var node))
                {
                    continue;
                }

                var caseBody = SyntaxFactory.Block();
                caseBody = node.AddStatementAndContinue(caseBody);

                // Handle default case
                if (outputWire.LeftSidePin.PinName == PinNames.DEFAULT_OUT)
                {
                    defaultCase = caseBody;
                    continue;
                }

                // Determine case label based on switch type
                SwitchLabelSyntax caseLabel;

                if (switchType.IsEnum)
                {
                    // Convert string to an enum member expression: MyEnum.SomeValue
                    caseLabel = SyntaxFactory.CaseSwitchLabel(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(switchType.Name), // Enum type name
                            SyntaxFactory.IdentifierName(outputWire.LeftSidePin.PinName) // Enum case
                        )
                    );
                }
                else if (switchType == typeof(string))
                {
                    // Use string literal case: case "SomeString":
                    caseLabel = SyntaxFactory.CaseSwitchLabel(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(outputWire.LeftSidePin.PinName)
                        )
                    );
                }
                else if (switchType == typeof(int))
                {
                    // Convert to an integer case: case 1:
                    if (int.TryParse(outputWire.LeftSidePin.PinName, out int intValue))
                    {
                        caseLabel = SyntaxFactory.CaseSwitchLabel(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(intValue)
                            )
                        );
                    }
                    else
                    {
                        continue; // Skip invalid integer cases
                    }
                }
                else
                {
                    continue; // Skip unsupported types
                }

                // Create the switch section with the case label and body
                var switchSection = SyntaxFactory.SwitchSection()
                    .AddLabels(caseLabel)
                    .AddStatements(caseBody.Statements.Concat(new[] { SyntaxFactory.BreakStatement() }).ToArray());

                cases.Add(switchSection);
            }

            // Add default case if provided
            if (defaultCase != null)
            {
                cases.Add(
                    SyntaxFactory.SwitchSection()
                        .AddLabels(SyntaxFactory.DefaultSwitchLabel())
                        .AddStatements(defaultCase.Statements.Concat(new[] { SyntaxFactory.BreakStatement() }).ToArray())
                );
            }

            // Create the switch statement
            var switchStatement = SyntaxFactory.SwitchStatement(SyntaxFactory.IdentifierName(switchVariableName))
                .AddSections(cases.ToArray());

            // Add the switch statement to the existing block and return
            block = block.AddStatements(switchStatement);
            return block;
        }
    }

    public class SequenceSyntaxNode : BlueprintSyntaxNodeBase
    {
        public SequenceSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            for (int i = 0; i < MyNodeController.OutputWires.Count; i++)
            {
                var pinName = $"{PinNames.SEQUENCE_OUT}_{i}";
                var wire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == pinName);
                if (!wire.IsValid() || !BlueprintScriptWriter.NodeMap.TryGetValue(wire.RightSidePin.NodeGuid, out var node))
                {
                    continue;
                }

                var b = SyntaxFactory.Block();
                b = node.AddStatementAndContinue(b);
                block = block.AddStatements(b);
            }

            // foreach (var outputWire in MyNodeController.Model.OutputWires)
            // {
            //     if (!BlueprintScriptWriter.NodeMap.TryGetValue(outputWire.RightSidePin.NodeGuid, out var node))
            //     {
            //         continue;
            //     }
            //
            //     var b = SyntaxFactory.Block();
            //     b = node.AddStatementAndContinue(b);
            //     block = block.AddStatements(b);
            // }
            return block;
        }
    }

    public class ForSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ForSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }
        
        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            // Create a for statement
            var arrayVarName = FormatWithUid(PinNames.ARRAY_IN);
            var indexEqualClauseName = FormatWithUid(PinNames.START_INDEX_IN);
            var lengthVarName = FormatWithUid(PinNames.LENGTH_IN);
            
            var indexVarName = FormatWithUid(PinNames.INDEX_OUT);
            var elementVarName = FormatWithUid(PinNames.ELEMENT_OUT);
            
            // Eval loop
            BlockSyntax loopBlock = SyntaxFactory.Block();
            // Create 'var element = array[index];' statement
            var elementAssignment = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .AddVariables(SyntaxFactory.VariableDeclarator(elementVarName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(arrayVarName))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(indexVarName))
                                        )
                                    )
                                )
                        ))
                    )
            );
            loopBlock = loopBlock.AddStatements(elementAssignment);
            
            var loopWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.LOOP_OUT);
            if (loopWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(loopWire.RightSidePin.NodeGuid, out var node))
            {
                loopBlock = node.AddStatementAndContinue(loopBlock);
            }
            
            // Create the for loop: for (int index = START_INDEX_IN; index < LENGTH_IN; index++)
            var forLoop = SyntaxFactory.ForStatement(loopBlock)
                .WithDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))) // int index
                        .AddVariables(SyntaxFactory.VariableDeclarator(indexVarName)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(indexEqualClauseName))) // index = START_INDEX_IN
                        )
                )
                .WithCondition(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LessThanExpression,
                        SyntaxFactory.IdentifierName(indexVarName),
                        SyntaxFactory.IdentifierName(lengthVarName) // index < LENGTH_IN
                    )
                )
                .WithIncrementors(
                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.PostfixUnaryExpression(
                            SyntaxKind.PostIncrementExpression,
                            SyntaxFactory.IdentifierName(indexVarName) // index++
                        )
                    )
                );
            
            // Add the switch statement to the existing block and return
            block = block.AddStatements(forLoop);
            
            // Continue
            var completeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.COMPLETE_OUT);
            if (completeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(completeWire.RightSidePin.NodeGuid, out var completeNode))
            {
                block = completeNode.AddStatementAndContinue(block);
            }
            
            return block;
        }
    }

    public class ForEachSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ForEachSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }
        
        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            // Create a for statement
            string arrayVarName = FormatWithUid(PinNames.ARRAY_IN);
            string elementVarName = FormatWithUid(PinNames.ELEMENT_OUT);
            
            // Eval loop
            BlockSyntax loopBlock = SyntaxFactory.Block();
            var loopWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.LOOP_OUT);
            if (loopWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(loopWire.RightSidePin.NodeGuid, out var node))
            {
                loopBlock = node.AddStatementAndContinue(loopBlock);
            }
            
            // Create foreach statement: foreach (var elementVarName in arrayVarName)
            var foreachLoop = SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"), // var elementVarName
                SyntaxFactory.Identifier(elementVarName),
                SyntaxFactory.IdentifierName(arrayVarName), // in arrayVarName
                loopBlock
            );

            // Add the foreach loop to the block
            block = block.AddStatements(foreachLoop);
            
            // Continue
            var completeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.COMPLETE_OUT);
            if (completeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(completeWire.RightSidePin.NodeGuid, out var completeNode))
            {
                block = completeNode.AddStatementAndContinue(block);
            }
            
            return block;
        }
    }

    public class WhileSyntaxNode : BlueprintSyntaxNodeBase
    {
        public WhileSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;

            // Create a for statement
            var conditionVarName = FormatWithUid(PinNames.VALUE_IN);
            
            // Eval loop
            BlockSyntax loopBlock = SyntaxFactory.Block();
            var loopWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.LOOP_OUT);
            if (loopWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(loopWire.RightSidePin.NodeGuid, out var node))
            {
                loopBlock = node.AddStatementAndContinue(loopBlock);
            }

            var whileStatement = SyntaxFactory.WhileStatement(SyntaxFactory.IdentifierName(conditionVarName), loopBlock);

            // Add the foreach loop to the block
            block = block.AddStatements(whileStatement);
            
            // Continue
            var completeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.COMPLETE_OUT);
            if (completeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(completeWire.RightSidePin.NodeGuid, out var completeNode))
            {
                block = completeNode.AddStatementAndContinue(block);
            }
            
            return block;
        }
    }
    
    public class BreakSyntaxNode : BlueprintSyntaxNodeBase
    {
        public BreakSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }
            IsEvaluated = true;
            block = block.AddStatements(SyntaxFactory.BreakStatement());
            return block;
        }
    }

    public class ContinueSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ContinueSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }
        
        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }
            IsEvaluated = true;
            block = block.AddStatements(SyntaxFactory.ContinueStatement());
            return block;
        }
    }

    public class ConversionSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ConversionSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            var nameOfObjToCast = FormatWithUid(MyNodeController.InputPins[PinNames.SET_IN].PortName);
            var typeToCast = MyNodeController.OutputPins[PinNames.GET_OUT].Type;
            var nameOfObjToSet = FormatWithUid(MyNodeController.OutputPins[PinNames.GET_OUT].PortName);

            ExpressionSyntax valueExpression;

            if (typeToCast == typeof(string))
            {
                // Generate: nameOfObjToCast.ToString()
                valueExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameOfObjToCast),
                        SyntaxFactory.IdentifierName("ToString")
                    )
                );
            }
            else
            {
                // Generate: (TargetType) nameOfObjToCast
                valueExpression = SyntaxFactory.CastExpression(
                    SyntaxFactory.ParseTypeName(typeToCast.FullName!), // Target type
                    SyntaxFactory.IdentifierName(nameOfObjToCast) // Object being cast
                );
            }

            // Create an assignment statement: var nameOfObjToSet = valueExpression;
            var assignmentStatement = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var")) // Use 'var'
                    .AddVariables(SyntaxFactory.VariableDeclarator(nameOfObjToSet)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(valueExpression)) // Assign cast or ToString() value
                    ));


            // // Create a cast expression: (TargetType) nameOfObjToCast
            // var castExpression = SyntaxFactory.CastExpression(
            //     SyntaxFactory.ParseTypeName(typeToCast.FullName!), // Target type
            //     SyntaxFactory.IdentifierName(nameOfObjToCast) // Object being cast
            // );
            //
            // // Create an assignment statement: var nameOfObjToSet = (TargetType) nameOfObjToCast;
            // var assignmentStatement = SyntaxFactory.LocalDeclarationStatement(
            //     SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var")) // Use 'var'
            //         .AddVariables(SyntaxFactory.VariableDeclarator(nameOfObjToSet)
            //                 .WithInitializer(SyntaxFactory.EqualsValueClause(castExpression)) // Assign cast value
            //         )
            // );

            // Add the assignment statement to the block
            block = block.AddStatements(assignmentStatement);
            return block;
        }
    }

    public class CastSyntaxNode : BlueprintSyntaxNodeBase
    {
        public CastSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (IsEvaluated)
            {
                Debug.LogError("Node is already evaluated");
                return block;
            }

            block = GetInputPinStatements(block);
            // block = CreateActionStatement(block);
            block = SetOutputPinStatements(block);
            IsEvaluated = true;
            
            var inputName = FormatWithUid(MyNodeController.InputPins[PinNames.VALUE_IN].PortName);
            var outputName = FormatWithUid(MyNodeController.OutputPins[PinNames.AS_OUT].PortName);
            var asType = MyNodeController.OutputPins[PinNames.AS_OUT].Type;
            
            var ifBlock = SyntaxFactory.Block();
            var elseBlock = SyntaxFactory.Block();

            var validNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.VALID_OUT);
            if (validNodeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(validNodeWire.RightSidePin.NodeGuid, out var ifNode))
            {
                ifBlock = ifNode.AddStatementAndContinue(ifBlock);
            }

            var invalidNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.LeftSidePin.PinName == PinNames.INVALID_OUT);
            if (invalidNodeWire.IsValid() && BlueprintScriptWriter.NodeMap.TryGetValue(invalidNodeWire.RightSidePin.NodeGuid, out var elseNode))
            {
                elseBlock = elseNode.AddStatementAndContinue(elseBlock);
            }

            // Create the type-checking expression: "inputName is OutputType outputName"
            var isExpression = SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName(inputName), // The variable being checked
                SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.ParseTypeName(asType.FullName!), // Type name
                    SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(outputName)) // Variable to assign the cast value
                )
            );

            // Create the if-else statement: if (inputName is OutputType outputName) { ifBlock } else { elseBlock }
            IfStatementSyntax ifElseStatement = SyntaxFactory.IfStatement(isExpression, ifBlock)
                .WithElse(SyntaxFactory.ElseClause(elseBlock));

            block = block.AddStatements(ifElseStatement);
            return block;
        }
    }

    public class RedirectSyntaxNode : BlueprintSyntaxNodeBase
    {
        public RedirectSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        public override BlockSyntax AddStatementAndContinue(BlockSyntax block)
        {
            if (!MyNodeController.OutputPins.ContainsKey(PinNames.EXECUTE_OUT))
            {
                return block;
            }

            var nextNodeWire = MyNodeController.OutputWires.FirstOrDefault(w => w.IsExecuteWire);
            if (!nextNodeWire.IsValid() || !BlueprintScriptWriter.NodeMap.TryGetValue(nextNodeWire.RightSidePin.NodeGuid, out var nextNode))
            {
                Debug.LogError($"Invalid Node Guid: {nextNodeWire.RightSidePin.PinName}");
                return block;
            }

            block = nextNode.AddStatementAndContinue(block);

            return block;
        }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            if (!MyNodeController.OutputPins.TryGetValue(PinNames.GET_OUT, out var pin))
            {
                return block;
            }

            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
                    .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithUid(pin.PortName))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(FormatWithUid(PinNames.SET_IN))))) // Assign generated value
            );
            block = block.AddStatements(variableDeclaration);

            return block;
        }
    }

    public class ConstructorSyntaxNode : BlueprintSyntaxNodeBase
    {
        public ConstructorSyntaxNode(NodeModelBase nodeController) : base(nodeController)
        {
        }

        protected override BlockSyntax SetOutputPinStatements(BlockSyntax block)
        {
            var pin = MyNodeController.OutputPins[PinNames.RETURN];
            
            // Use Default Value
            var defaultValue = pin.InlineValue == null ? BlueprintScriptWriter.GetDefaultValueSyntax(pin.Type) : BlueprintScriptWriter.GetExpressionForObjectInitializer(pin.InlineValue.Get());

            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(BlueprintScriptWriter.GetTypeSyntax(pin.Type))
                    .AddVariables(SyntaxFactory.VariableDeclarator(FormatWithUid(PinNames.RETURN))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(defaultValue))) // Assign generated value pin. PortName
            );
            block = block.AddStatements(variableDeclaration);
            return block;
        }
    }
}
