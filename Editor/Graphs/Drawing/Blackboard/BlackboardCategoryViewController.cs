//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UIElements;
//using Vapor.UI;

//namespace VaporEditor.Graphs
//{
//    public class BlackboardCategoryViewController : UIViewController<CategoryData, BlackboardCategoryViewModel>, IDisposable
//    {
//        public BlackboardCategory blackboardCategoryView => m_BlackboardCategoryView;
//        BlackboardCategory m_BlackboardCategoryView;
//        Dictionary<string, BlackboardItemController> m_BlackboardItemControllers = new Dictionary<string, ShaderInputViewController>();
//        Blackboard blackboard { get; set; }

//        Action m_UnregisterAll;

//        public BlackboardCategoryViewController(CategoryData categoryData, BlackboardCategoryViewModel categoryViewModel, ISourceDataStore dataStore)
//            : base(categoryData, categoryViewModel, dataStore)
//        {
//            m_BlackboardCategoryView = new BlackboardCategory(categoryViewModel, this);
//            blackboard = categoryViewModel.ParentView as Blackboard;
//            if (blackboard == null)
//                return;

//            blackboard.Add(m_BlackboardCategoryView);

//            var dragActionCancelCallback = new EventCallback<MouseUpEvent>((evt) =>
//            {
//                m_BlackboardCategoryView.OnDragActionCanceled();
//            });

//            m_UnregisterAll += () => blackboard.UnregisterCallback(dragActionCancelCallback);

//            // These make sure that the drag indicators are disabled whenever a drag action is cancelled without completing a drop
//            blackboard.RegisterCallback(dragActionCancelCallback);
//            blackboard.hideDragIndicatorAction += m_BlackboardCategoryView.OnDragActionCanceled;

//            foreach (var categoryItem in categoryData.Children)
//            {
//                if (categoryItem == null)
//                {
//                    Debug.LogError("Failed to insert blackboard row into category due to shader input being null.");
//                    continue;
//                }
//                InsertBlackboardRow(categoryItem);
//            }
//        }

//        protected override void RequestModelChange(ISourceDataAction changeAction)
//        {
//            DataStore.Dispatch(changeAction);
//        }

//        // Called by GraphDataStore.Subscribe after the model has been changed
//        protected override void ModelChanged(object graphData, ISourceDataAction changeAction)
//        {
//            // If categoryData associated with this controller is removed by an operation, destroy controller and views associated
//            if (graphData.ContainsCategory(Model) == false)
//            {
//                Dispose();
//                return;
//            }

//            switch (changeAction)
//            {
//                case AddShaderInputAction addBlackboardItemAction:
//                    if (addBlackboardItemAction.shaderInputReference != null && IsInputInCategory(addBlackboardItemAction.shaderInputReference))
//                    {
//                        var blackboardRow = FindBlackboardRow(addBlackboardItemAction.shaderInputReference);
//                        if (blackboardRow == null)
//                            blackboardRow = InsertBlackboardRow(addBlackboardItemAction.shaderInputReference);
//                        // Rows should auto-expand when an input is first added
//                        // blackboardRow.expanded = true;
//                        var propertyView = blackboardRow.Q<SGBlackboardField>();
//                        if (addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
//                            propertyView.OpenTextEditor();
//                    }
//                    break;

//                case CopyShaderInputAction copyShaderInputAction:
//                    // In the specific case of only-one keywords like Material Quality and Raytracing, they can get copied, but because only one can exist, the output copied value is null
//                    if (copyShaderInputAction.copiedShaderInput != null && IsInputInCategory(copyShaderInputAction.copiedShaderInput))
//                    {
//                        var blackboardRow = InsertBlackboardRow(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);
//                        if (blackboardRow != null)
//                        {
//                            var graphView = ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
//                            var propertyView = blackboardRow.Q<SGBlackboardField>();
//                            graphView?.AddToSelectionNoUndoRecord(propertyView);
//                        }
//                    }
//                    break;

//                case AddItemToCategoryAction addItemToCategoryAction:
//                    // If item was added to category that this controller manages, then add blackboard row to represent that item
//                    if (addItemToCategoryAction.itemToAdd != null && addItemToCategoryAction.categoryGuid == ViewModel.associatedCategoryGuid)
//                    {
//                        InsertBlackboardRow(addItemToCategoryAction.itemToAdd, addItemToCategoryAction.indexToAddItemAt);
//                    }
//                    else
//                    {
//                        // If the added input has been added to a category other than this one, and it used to belong to this category,
//                        // Then cleanup the controller and view that used to represent that input
//                        foreach (var key in m_BlackboardItemControllers.Keys)
//                        {
//                            var blackboardItemController = m_BlackboardItemControllers[key];
//                            if (blackboardItemController.Model == addItemToCategoryAction.itemToAdd)
//                            {
//                                RemoveBlackboardRow(addItemToCategoryAction.itemToAdd);
//                                break;
//                            }
//                        }
//                    }
//                    break;

//                case DeleteCategoryAction deleteCategoryAction:
//                    if (deleteCategoryAction.categoriesToRemoveGuids.Contains(ViewModel.AssociatedCategoryGuid))
//                    {
//                        this.Dispose();
//                        return;
//                    }

//                    break;

//                case ChangeCategoryIsExpandedAction changeIsExpandedAction:
//                    if (changeIsExpandedAction.CategoryGuids.Contains(ViewModel.AssociatedCategoryGuid))
//                    {
//                        ViewModel.IsExpanded = changeIsExpandedAction.IsExpanded;
//                        m_BlackboardCategoryView.TryDoFoldout(changeIsExpandedAction.IsExpanded);
//                    }
//                    break;

//                case ChangeCategoryNameAction changeCategoryNameAction:
//                    if (changeCategoryNameAction.CategoryGuid == ViewModel.AssociatedCategoryGuid)
//                    {
//                        ViewModel.Name = Model.Name;
//                        m_BlackboardCategoryView.title = ViewModel.Name;
//                    }
//                    break;
//            }
//        }

//        public bool IsInputInCategory(ShaderInput shaderInput)
//        {
//            return Model != null && Model.IsItemInCategory(shaderInput);
//        }

//        public BlackboardRow FindBlackboardRow(ShaderInput shaderInput)
//        {
//            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var associatedController);
//            return associatedController?.BlackboardItemView;
//        }

//        // Creates controller, view and view model for a blackboard item and adds the view to the specified index in the category
//        // By default adds it to the end of the list if no insertionIndex specified
//        public BlackboardRow InsertBlackboardRow(BlackboardItem shaderInput, int insertionIndex = -1)
//        {
//            var shaderInputViewModel = new ShaderInputViewModel()
//            {
//                model = shaderInput,
//                parentView = blackboardCategoryView,
//            };
//            var blackboardItemController = new BlackboardItemController(shaderInput, shaderInputViewModel, DataStore);

//            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var existingItemController);
//            if (existingItemController == null)
//            {
//                m_BlackboardItemControllers.Add(shaderInput.objectId, blackboardItemController);
//                // If no index specified, or if trying to insert at last index, add to end of category
//                if (insertionIndex == -1 || insertionIndex == m_BlackboardItemControllers.Count() - 1)
//                    blackboardCategoryView.Add(blackboardItemController.BlackboardItemView);
//                else
//                    blackboardCategoryView.Insert(insertionIndex, blackboardItemController.BlackboardItemView);

//                blackboardCategoryView.MarkDirtyRepaint();

//                return blackboardItemController.BlackboardItemView;
//            }
//            else
//            {
//                Debug.LogError("Tried to add blackboard item that already exists to category.");
//                return null;
//            }
//        }

//        public void RemoveBlackboardRow(BlackboardItem shaderInput)
//        {
//            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var associatedBlackboardItemController);
//            if (associatedBlackboardItemController != null)
//            {
//                associatedBlackboardItemController.Dispose();
//                m_BlackboardItemControllers.Remove(shaderInput.objectId);
//            }
//            else
//            {
//                Debug.LogError("Failed to find associated blackboard item controller for shader input that was just deleted. Cannot clean up view associated with input.");
//            }
//        }

//        void ClearBlackboardRows()
//        {
//            foreach (var shaderInputViewController in m_BlackboardItemControllers.Values)
//            {
//                shaderInputViewController.Dispose();
//            }

//            m_BlackboardItemControllers.Clear();
//        }

//        public override void Dispose()
//        {
//            if (blackboard == null)
//            {
//                return;
//            }

//            base.Dispose();
//            Cleanup();
//            ClearBlackboardRows();
//            m_UnregisterAll?.Invoke();

//            blackboard = null;
//            m_BlackboardCategoryView?.Dispose();
//            m_BlackboardCategoryView?.Clear();
//            m_BlackboardCategoryView = null;
//        }
//    }
//}
