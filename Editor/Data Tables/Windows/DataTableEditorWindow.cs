using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor;
using Vapor.DataTables;
using Vapor.Inspector;
using Vapor.NewtonsoftConverters;
using VaporEditor.Inspector;
using ClipboardUtility = VaporEditor.Inspector.ClipboardUtility;
using TextElement = UnityEngine.UIElements.TextElement;

namespace VaporEditor.DataTables
{
    public class DataTableEditorWindow : EditorWindow
    {
        public string SelectedGuid { get; private set; }

        private Dictionary<string, DataTableRowDefinition> _deserializedData;

        private Dictionary<string, DataTableRowDefinition> Data
        {
            get
            {
                if (_deserializedData != null)
                {
                    return _deserializedData;
                }

                _deserializedData = new Dictionary<string, DataTableRowDefinition>();
                foreach (var rowData in _dataTable.RuntimeData)
                {
                    _deserializedData[rowData.RowHandle] = rowData;
                }

                return _deserializedData;
            }
        }

        private DataTableSo _dataTable;
        private VisualElement _listHeader;
        private ListView _listView;
        private VisualElement _detailsPanel;
        private InspectorTreeObject _inspectorTreeObject;

        private readonly Dictionary<int, float> _columnWidthsMap = new();
        private Label _headerLabel;
        private List<string> _filteredKeys;
        private ToolbarSearchField _searchField;

        #region - Setup - Update - Teardown -

        public void Initialize(string assetGuid)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DataTableSo>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (asset == null)
            {
                Debug.Log("Initialized: Asset null");
                return;
            }

            if (!EditorUtility.IsPersistent(asset))
            {
                Debug.Log("Initialized: Asset not persistant");
                return;
            }

            if (SelectedGuid == assetGuid)
            {
                Debug.Log($"Initialized: Already initialized: {asset.name}");
                return;
            }

            Debug.Log($"Initialized: {asset.GetType()}");
            SelectedGuid = assetGuid;
            _dataTable = asset;

            Debug.Log("Initialize");
            CreateGUIAfterInit();
            LoadDataTable(_dataTable);
        }

        private void CreateGUIAfterInit()
        {
            var root = rootVisualElement;
            root.name = "DataTableView";

            var styleSheet = LoadStyleSheet("DataTableView");
            root.styleSheets.Add(styleSheet);
            root.styleSheets.Add(!EditorGUIUtility.isProSkin ? LoadStyleSheet("DataTableView-light") : LoadStyleSheet("DataTableView-dark"));

            var toolbar = new Toolbar();
            var saveButton = new ToolbarButton(SaveData)
            {
                tooltip = "Save"
            };
            saveButton.Add(new Image { image = EditorGUIUtility.IconContent("SaveActive").image });
            toolbar.Add(saveButton);

            var addButton = new ToolbarButton(AddRow)
            {
                tooltip = "Add Row"
            };
            addButton.Add(new Image { image = EditorGUIUtility.IconContent("d_Toolbar Plus").image });
            toolbar.Add(addButton);

            _searchField = new ToolbarSearchField()
            {
                tooltip = "Search",
            };
            _searchField.RegisterValueChangedCallback(OnSearchChanged);
            toolbar.Add(_searchField);


            toolbar.Add(new ToolbarSpacer
            {
                style =
                {
                    flexGrow = 1f
                }
            });

            var exportJsonButton = new ToolbarButton(ExportToJson)
            {
                tooltip = "Export to JSON"
            };
            exportJsonButton.Add(new Image { image = Resources.Load<Sprite>("Icons/json_export").texture });
            toolbar.Add(exportJsonButton);

            var exportCsvButton = new ToolbarButton(ExportToCsv)
            {
                tooltip = "Export to CSV"
            };
            exportCsvButton.Add(new Image { image = Resources.Load<Sprite>("Icons/csv_export").texture });
            toolbar.Add(exportCsvButton);

            var importCsvButton = new ToolbarButton(ImportFromJson)
            {
                tooltip = "Import JSON"
            };
            importCsvButton.Add(new Image { image = EditorGUIUtility.IconContent("d_Import").image });

            toolbar.Add(importCsvButton);

            root.Add(toolbar);

            _listView = new ListView
            {
                makeItem = () => new VisualElement(),
                bindItem = (element, index) =>
                {
                    if (!_dataTable || index >= Data.Count)
                    {
                        return;
                    }

                    string key = _filteredKeys.ElementAt(index);
                    DataTableRowDefinition rowData = Data[key];

                    element.Clear();
                    var rowContainer = new VisualElement
                    {
                        style =
                        {
                            flexGrow = 1f,
                            flexDirection = FlexDirection.Row
                        }
                    };

                    // Key Column
                    var keyLabel = new Label(key)
                    {
                        style =
                        {
                            minWidth = _columnWidthsMap[0],
                            unityTextAlign = TextAnchor.MiddleLeft,
                            marginLeft = 6,
                        }
                    };
                    rowContainer.Add(keyLabel);

                    // Value Columns
                    var data = rowData.PrintColumnData().ToArray();
                    int idx = 0;
                    int count = data.Length;
                    foreach (var field in data)
                    {
                        string value = !field.EmptyOrNull() ? field : "N/A";
                        var valueLabel = new Label(value)
                        {
                            style =
                            {
                                borderLeftWidth = 1,
                                borderLeftColor = Color.grey,
                                borderRightWidth = idx == count - 1 ? 1 : 0,
                                borderRightColor = Color.grey,
                                minWidth = _columnWidthsMap[idx + 1],
                                unityTextAlign = TextAnchor.MiddleCenter
                            }
                        };
                        rowContainer.Add(valueLabel);
                        idx++;
                    }

                    rowContainer.AddManipulator(new ContextualMenuManipulator(evt =>
                    {
                        evt.menu.AppendAction("Cut", _ => { CutRow(rowContainer.parent.parent.IndexOf(rowContainer.parent)); });
                        evt.menu.AppendAction("Copy", _ => { CopyRow(rowContainer.parent.parent.IndexOf(rowContainer.parent)); });
                        evt.menu.AppendAction("Paste", _ => { PasteRow(rowContainer.parent.parent.IndexOf(rowContainer.parent)); },
                            _ => ClipboardUtility.CanReadFromBuffer(typeof(string)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                        evt.menu.AppendAction("Duplicate", _ => { DuplicateRow(rowContainer.parent.parent.IndexOf(rowContainer.parent)); });
                        evt.menu.AppendAction("Delete", _ => { DeleteRow(rowContainer.parent.parent.IndexOf(rowContainer.parent)); });
                        evt.menu.AppendSeparator();
                        evt.menu.AppendAction("Reset", _ => { ResetRow(rowContainer.parent.parent.IndexOf(rowContainer.parent)); });
                    }));

                    element.Add(rowContainer);
                    element.RegisterCallbackOnce<GeometryChangedEvent>(OnListGeometryChanged);

                },
                selectionType = SelectionType.Single,
                horizontalScrollingEnabled = true,
                style =
                {
                    minWidth = 120,
                    flexGrow = 1
                }
            };

            var splitView = new TwoPaneSplitView(0, 340, TwoPaneSplitViewOrientation.Vertical);
            root.Add(splitView);
            _listView.selectionChanged += ShowRowDetails;
            _listView.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            _listView.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            var topGroup = new VisualElement { style = { flexGrow = 1 } };
            _listHeader = MakeHeaderLabels();
            _listView.Q<ScrollView>().verticalScroller.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.target is VisualElement ve && _listHeader is ScrollView sv)
                {
                    sv.style.marginRight = ve.style.display == DisplayStyle.None ? 0f : 13f;
                }
            });
            _listView.Q<ScrollView>().horizontalScroller.valueChanged += x =>
            {
                if (_listHeader is ScrollView sv)
                {
                    sv.horizontalScroller.value = x;
                }
            };
            topGroup.Add(_listHeader);
            topGroup.Add(_listView);
            splitView.Add(topGroup);

            _detailsPanel = new ScrollView() { style = { flexGrow = 1 } };
            splitView.Add(_detailsPanel);
        }

        private VisualElement MakeHeaderLabels()
        {
            var type = _dataTable.GetRowType();
            var element = new ScrollView(ScrollViewMode.Horizontal)
            {
                name = "HeaderRow",
                horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
                style =
                {
                    flexGrow = 0f,
                    flexShrink = 0f,
                    minHeight = 28f,
                },
                horizontalScroller =
                {
                    visible = false,
                    style =
                    {
                        maxHeight = 2f
                    }
                }
            };
            var rowContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    flexDirection = FlexDirection.Row
                }
            };

            // Value Columns
            var fields = ReflectionUtility.GetAllFieldsThatMatch(type, fi => !fi.IsPublic && !fi.IsStatic && fi.IsDefined(typeof(SerializeField), false), false);
            int idx = 0;
            int count = fields.Count;
            foreach (var field in fields)
            {
                string value = !field.Name.EmptyOrNull() ? ObjectNames.NicifyVariableName(field.Name) : "N/A";
                var valueLabel = new Label(value)
                {
                    style =
                    {
                        borderLeftWidth = 1,
                        borderLeftColor = Color.grey,
                        borderBottomWidth = 1,
                        borderBottomColor = Color.grey,
                        borderRightWidth = idx == count - 1 ? 1 : 0,
                        borderRightColor = Color.grey,
                        backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                        minWidth = 100,
                        unityTextAlign = TextAnchor.MiddleCenter,
                    }
                };
                if (idx == 0)
                {
                    _headerLabel = valueLabel;
                    _headerLabel.RegisterCallbackOnce<GeometryChangedEvent>(OnResolveTextElement);
                }

                rowContainer.Add(valueLabel);
                idx++;
            }

            element.Add(rowContainer);
            return element;
        }

        private void LoadDataTable(DataTableSo dataTable)
        {
            if (!dataTable)
            {
                return;
            }

            _dataTable = dataTable;
            _deserializedData?.Clear();
            _filteredKeys = new List<string>(Data.Keys);
            SizeColumns();
            RebuildList();
        }

        private void OnEnable()
        {
            this.SetAntiAliasing(4);
        }

        private void OnDisable()
        {
            _dataTable = null;
            Resources.UnloadUnusedAssets();
        }

        private void Update()
        {
            if (!_dataTable && SelectedGuid != null)
            {
                var guid = SelectedGuid;
                SelectedGuid = null;
                Initialize(guid);
            }

            if (!_dataTable)
            {
                Close();
            }
        }

        #endregion

        #region - Event Callbacks -

        private void ShowRowDetails(IEnumerable<object> selected)
        {
            _detailsPanel.Clear();
            var arr = selected.ToList();
            if (arr.Count == 0 || !_dataTable)
            {
                return;
            }

            string rowKey = arr[0] as string;
            if (rowKey.EmptyOrNull())
            {
                return;
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            if (!Data.TryGetValue(rowKey, out var row))
            {
                return;
            }

            var detail = SerializedDrawerUtility.DrawFieldFromObject(row, row.GetType());
            detail.RegisterCallback<TreePropertyChangedEvent>(OnRowDetailChanged);
            _detailsPanel.Add(detail);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            RebuildList();
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case "Cut" when _listView.selectedIndices.Any():
                case "Copy" when _listView.selectedIndices.Any():
                case "Delete" when _listView.selectedIndices.Any():
                case "SoftDelete" when _listView.selectedIndices.Any():
                case "Duplicate" when _listView.selectedIndices.Any():
                case "Paste" when ClipboardUtility.CanReadFromBuffer(typeof(string)):
                    evt.StopPropagation();
                    break;
            }
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            Debug.Log(evt.commandName);
            switch (evt.commandName)
            {
                case "Cut":
                    CutRow(_listView.selectedIndex);
                    evt.StopPropagation();
                    break;
                case "Copy":
                    CopyRow(_listView.selectedIndex);
                    evt.StopPropagation();
                    break;
                case "Paste":
                    PasteRow(_listView.selectedIndex);
                    evt.StopPropagation();
                    break;
                case "Delete":
                case "SoftDelete":
                    DeleteRow(_listView.selectedIndex);
                    evt.StopPropagation();
                    break;
                case "Duplicate":
                    DuplicateRow(_listView.selectedIndex);
                    evt.StopPropagation();
                    break;
            }
        }

        private void OnResolveTextElement(GeometryChangedEvent evt)
        {
            SizeColumns();
        }

        private void OnListGeometryChanged(GeometryChangedEvent evt)
        {
            var content = _listView.Q<ScrollView>().contentContainer;
            int rowCount = content.childCount;
            if (rowCount == 0)
            {
                return;
            }

            foreach (var visualElement in content.Children())
            {
                int columnCount = visualElement[0].childCount;
                if (columnCount == 0)
                {
                    return;
                }

                for (int i = 0; i < columnCount; i++)
                {
                    float margins = i == 0 ? 6f : 0f;
                    var headerLabel = (Label)_listHeader[0][i];
                    var minWidth = _columnWidthsMap[i];
                    headerLabel.style.minWidth = minWidth + margins;
                    for (int j = 0; j < rowCount; j++)
                    {
                        content[j][0][i].style.minWidth = minWidth;
                    }
                }
            }
        }

        private void OnRowDetailChanged(TreePropertyChangedEvent evt)
        {
            if (evt.target is TreePropertyField field && field.Property.PropertyName == "Handle")
            {
                SaveData();
                RebuildData();

                RebuildList();
            }
        }

        #endregion

        #region - Row Actions -

        private void AddRow()
        {
            if (!_dataTable)
            {
                return;
            }

            string rowName = "NewRow";
            string originalName = rowName;
            int index = 0;
            do
            {
                rowName = $"{originalName}_{index++}";
            } while (Data.ContainsKey(rowName));

            var row = (DataTableRowDefinition)Activator.CreateInstance(_dataTable.GetRowType());
            row.RowHandle = rowName;
            Data.Add(rowName, row);

            SaveData();
        }

        private void DeleteRow(int index)
        {
            if (!_dataTable || index < 0 || index >= _listView.itemsSource.Count)
            {
                return;
            }

            var rowHandle = _listView.itemsSource[index];
            Data.Remove(rowHandle.ToString());
            SaveData();
        }

        private void CutRow(int index)
        {
            if (!_dataTable || index < 0 || index >= _listView.itemsSource.Count)
            {
                return;
            }

            var rowHandle = _listView.itemsSource[index];
            var dup = RowToJson(rowHandle.ToString());
            ClipboardUtility.WriteToBuffer(dup);

            Data.Remove(rowHandle.ToString());
            SaveData();
        }

        private void CopyRow(int index)
        {
            if (!_dataTable || index < 0 || index >= _listView.itemsSource.Count)
            {
                return;
            }

            var rowHandle = _listView.itemsSource[index];
            var dup = RowToJson(rowHandle.ToString());
            ClipboardUtility.WriteToBuffer(dup);
        }

        private void PasteRow(int index)
        {
            if (!_dataTable || index < 0 || index >= _listView.itemsSource.Count)
            {
                return;
            }

            var rowHandle = _listView.itemsSource[index];
            RowFromJson(rowHandle.ToString(), ClipboardUtility.CopyBuffer.ToString(), true);
            SaveData();
        }

        private void DuplicateRow(int index)
        {
            if (!_dataTable || index < 0 || index >= _listView.itemsSource.Count)
            {
                return;
            }

            var rowHandle = _listView.itemsSource[index];
            var dup = RowToJson(rowHandle.ToString());
            if (dup.EmptyOrNull())
            {
                return;
            }


            var newHandle = $"{rowHandle}".Split('_')[0];
            RowFromJson(newHandle, dup, false);
            SaveData();
        }

        private void ResetRow(int index)
        {
            if (!_dataTable || index < 0 || index >= _listView.itemsSource.Count)
            {
                return;
            }

            var rowHandle = _listView.itemsSource[index];
            string rowName = rowHandle.ToString();
            var row = (DataTableRowDefinition)Activator.CreateInstance(_dataTable.GetRowType());
            row.RowHandle = rowName;
            Data[rowName] = row;
            SaveData();
        }

        private string RowToJson(string rowName)
        {
            if (!Data.TryGetValue(rowName, out var rd))
            {
                return null;
            }

            var rows = new List<DataTableRowDefinition> { rd };
            return JsonConvert.SerializeObject(rows, NewtonsoftUtility.SerializerSettings);

        }

        private void RowFromJson(string rowName, string jsonData, bool overwrite)
        {
            var data = JsonConvert.DeserializeObject<List<DataTableRowDefinition>>(jsonData, NewtonsoftUtility.SerializerSettings);
            var d = data[0];
            if (!overwrite)
            {
                string originalName = rowName;
                int index = 0;
                do
                {
                    rowName = $"{originalName}_{index++}";
                } while (Data.ContainsKey(rowName));

            }

            d.RowHandle = rowName;
            Data[rowName] = d;
        }

        #endregion

        #region - Helpers -

        private void SizeColumns()
        {
            _columnWidthsMap.Clear();

            var columnFields = ReflectionUtility.GetAllFieldsThatMatch(_dataTable.GetRowType(), fi => !fi.IsPublic && !fi.IsStatic && fi.IsDefined(typeof(SerializeField), false), false);
            int idx = 0;
            foreach (var column in columnFields)
            {
                float columnMargin = idx == 0 ? 6f : 0f;
                var value = !column.Name.EmptyOrNull() ? ObjectNames.NicifyVariableName(column.Name) : "N/A";
                var columnWidth = DataTableEditorUtility.MeasureTextSize(value, _headerLabel).x + 12 + columnMargin;
                float headerWidth = Mathf.Max(columnWidth, 100);

                _columnWidthsMap[idx] = headerWidth;

                idx++;
            }

            idx = 0;
            foreach (var column in columnFields)
            {
                float width = 100;
                float columnMargin = idx == 0 ? 6f : 0f;
                foreach (var row in Data.Values)
                {
                    var value = column.GetValue(row)?.ToString();
                    value = !value.EmptyOrNull() ? ObjectNames.NicifyVariableName(value) : "N/A";
                    var columnWidth = DataTableEditorUtility.MeasureTextSize(value, _headerLabel).x + 12 + columnMargin;
                    width = Mathf.Max(columnWidth, width);
                }

                if (_columnWidthsMap[idx] < width)
                {
                    _columnWidthsMap[idx] = width;
                }

                idx++;
            }
        }

        private void SaveData()
        {
            if (!_dataTable)
            {
                return;
            }

            // JsonData = JsonConvert.SerializeObject(Data.Values.ToArray(), NewtonsoftUtility.SerializerSettings);
            _dataTable.RuntimeData.Clear();
            foreach (var a in Data.Values)
            {
                _dataTable.RuntimeData.Add(a);
            }

            RuntimeEditorUtility.DirtyAndSave(_dataTable);

            SizeColumns();
            RebuildList();
        }

        private void RebuildList()
        {
            string searchText = _searchField.value;
            _filteredKeys = new List<string>();
            if (searchText.EmptyOrNull())
            {
                _filteredKeys = new List<string>(Data.Keys);
            }
            else
            {
                _filteredKeys.AddRange(Data.Keys.Where(k => FuzzySearch.FuzzyMatch(searchText, k)));
            }

            _listView.itemsSource = new List<string>(_filteredKeys);
            _listView.Rebuild();
        }

        private void RebuildData()
        {
            if (!_dataTable)
            {
                return;
            }

            _deserializedData = null;
        }

        private static StyleSheet LoadStyleSheet(string text)
        {
            return Resources.Load<StyleSheet>($"Styles/{text}");
        }

        #endregion

        #region - Json Serializing -

        private void ExportToJson()
        {
            string path = EditorUtility.SaveFilePanel("Save JSON", "", "DataTable.json", "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            SaveData();
            string json = JsonConvert.SerializeObject(Data.Values.ToArray(), NewtonsoftUtility.SerializerSettings);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"Exported JSON to: {path}");
        }

        private void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Import File", "", "json,csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string fileExtension = Path.GetExtension(path).ToLower();
            string jsonContent;

            if (fileExtension == ".csv")
            {
                string csvContent = File.ReadAllText(path);
                jsonContent = ConvertCsvToJson(csvContent);
            }
            else if (fileExtension == ".json")
            {
                jsonContent = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError("Unsupported file format. Please select a JSON or CSV file.");
                return;
            }

            _deserializedData ??= new Dictionary<string, DataTableRowDefinition>();
            _deserializedData.Clear();

            var data = JsonConvert.DeserializeObject<List<DataTableRowDefinition>>(jsonContent, NewtonsoftUtility.SerializerSettings);
            foreach (var rowData in data)
            {
                _deserializedData[rowData.RowHandle] = rowData;
            }

            SaveData();
            RebuildList();
        }

        #endregion

        #region - CSV Serializing -

        private void ExportToCsv()
        {
            string path = EditorUtility.SaveFilePanel("Save CSV", "", "DataTable.csv", "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            SaveData();
            string json = JsonConvert.SerializeObject(Data.Values.ToArray(), NewtonsoftUtility.SerializerSettings);
            string csv = ConvertJsonToCsv(json);
            File.WriteAllText(path, csv);
            AssetDatabase.Refresh();
            Debug.Log($"Exported CSV to: {path}");
        }

        private static string ConvertJsonToCsv(string json)
        {
            // Parse JSON as an array
            JArray jsonArray = JArray.Parse(json);
            if (jsonArray.Count == 0)
                return string.Empty;

            // Extract headers dynamically from the first object
            var headers = jsonArray[0].Children<JProperty>().Select(p => p.Name).ToArray();

            // Create the CSV output
            var csvRows = new List<string>
            {
                string.Join(",", headers) // Add header row
            };

            // Convert JSON objects to CSV rows
            foreach (var jToken in jsonArray)
            {
                var obj = (JObject)jToken;
                var row = string.Join(",", headers.Select(header => EscapeCsvValue(obj[header]?.ToString(), obj[header]!.Type)));
                csvRows.Add(row);
            }

            // Return CSV content
            return string.Join("\n", csvRows);
        }

        private static string EscapeCsvValue(string value, JTokenType type)
        {
            if (value.EmptyOrNull())
            {
                return "null";
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r") || type == JTokenType.String)
            {
                // Escape double quotes by doubling them, then wrap the whole value in double quotes
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private static string ConvertCsvToJson(string csv)
        {
            var lines = ParseCsvLines(csv);
            if (lines.Count < 2) return "[]"; // Not enough data to process

            // First line = header, which contains the field names
            var headers = ParseCsvRow(lines[0]);
            var jsonObjects = new List<Dictionary<string, object>>();

            // Process each subsequent row
            for (int i = 1; i < lines.Count; i++)
            {
                var values = ParseCsvRow(lines[i]);
                if (values.Length == headers.Length)
                {
                    var jsonObject = new Dictionary<string, object>();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        jsonObject[headers[j]] = TryParseJson(values[j]);
                    }

                    jsonObjects.Add(jsonObject);
                }
            }
            
            StringBuilder sb = new StringBuilder();
            
            sb.Append("[\r\n");
            for (var index = 0; index < jsonObjects.Count; index++)
            {
                var element = jsonObjects[index];
                var list = element.Keys.ToList();
                sb.Append("\t{\r\n");
                for (var i = 0; i < list.Count; i++)
                {
                    var value = element[list[i]];
                    sb.Append(i == list.Count - 1 
                        ? $"\t\t\"{list[i]}\": {value}\r\n" 
                        : $"\t\t\"{list[i]}\": {value},\r\n");
                }

                sb.Append(index == jsonObjects.Count - 1 
                    ? "\t}\r\n" 
                    : "\t},\r\n");
            }

            sb.Append("]");
            return sb.ToString();
        }

        private static List<string> ParseCsvLines(string csv)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            bool insideQuotes = false;
            int insideBrackets = 0;
            int insideBraces = 0;

            int lineIdx = 0;
            var column = new List<string>();
            int columnCount = 0;
            foreach (var line in csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (lineIdx == 0)
                {
                    lines.Add(line);
                    columnCount = line.Split(',').Length;
                    lineIdx++;
                    continue;
                }
                
                // Check each line for quotes and commas to split or append to the current value
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == '{')
                    {
                        insideBrackets++;
                    }
                    if (line[i] == '}')
                    {
                        insideBrackets--;
                    }
                    if (line[i] == '[')
                    {
                        insideBraces++;
                    }
                    if (line[i] == ']')
                    {
                        insideBraces--;
                    }
                    
                    if (i == line.Length - 1 && insideBrackets == 0 && insideBraces == 0)
                    {
                        if (line[i] == '"')
                        {
                            insideQuotes = !insideQuotes;
                        }
                        
                        sb.Append(line[i]);
                        column.Add(sb.ToString().Trim());
                        sb.Clear();
                    }
                    else if (line[i] == '"')
                    {
                        insideQuotes = !insideQuotes;
                        sb.Append(line[i]); // Add the quote character to the value
                    }
                    else if (line[i] == ',' && !insideQuotes) // Split only if outside quotes
                    {
                        column.Add(sb.ToString().Trim());
                        sb.Clear(); // Reset for next field
                    }
                    else
                    {
                        sb.Append(line[i]); // Append character to the current field
                    }
                }
                
                // Add the last value in the line
                if (column.Count == columnCount)
                {
                    lines.Add(string.Join(",", column));
                    column.Clear();
                }
                
                lineIdx++;
            }

            return lines;
        }

        private static string[] ParseCsvRow(string row)
        {
            var values = new List<string>();
            var csvPattern = new Regex("(?:^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)", RegexOptions.Compiled);
            var matches = csvPattern.Matches(row);

            foreach (Match match in matches)
            {
                string value = match.Value.TrimStart(',');

                // if (value.StartsWith("\"") && value.EndsWith("\""))
                // {
                //     value = value.Substring(1, value.Length - 2).Replace("\"\"", "\""); // Unescape quotes
                // }

                values.Add(value);
            }

            return values.ToArray();
        }

        private static object TryParseJson(string value)
        {
            value = value.Trim();
            // If the value is a valid JSON object or array, deserialize it
            if ((value.StartsWith("\"{") && value.EndsWith("}\"")) || (value.StartsWith("\"[") && value.EndsWith("]\"")))
            {
                value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
                return value;
                // try
                // {
                //     return JsonConvert.DeserializeObject(value); // Parse JSON object or array
                // }
                // catch
                // {
                //     return value; // If parsing fails, return as a string
                // }
            }

            return value; // Return as string if not JSON
        }

        #endregion
    }

    public static class DataTableEditorUtility
    {
        private static TextElement CreateTextElementWithDefaults(VisualElement referenceElement)
        {
            var textElement = new TextElement();

            // Clone styles from a reference element (must be inside UI)
            if (referenceElement?.resolvedStyle?.unityFontDefinition != null)
            {
                textElement.style.unityFont = referenceElement.resolvedStyle.unityFont;
                textElement.style.unityFontDefinition = referenceElement.resolvedStyle.unityFontDefinition;
                textElement.style.fontSize = referenceElement.resolvedStyle.fontSize;
                textElement.style.unityFontStyleAndWeight = referenceElement.resolvedStyle.unityFontStyleAndWeight;
            }
            else
            {
                // Fallback to manually assigning default font
                textElement.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textElement.style.fontSize = 14; // Default size
            }

            return textElement;
        }

        public static Vector2 MeasureTextSize(string text, VisualElement referenceElement)
        {
            var textElement = CreateTextElementWithDefaults(referenceElement);
            textElement.text = text;

            return textElement.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        }
    }
}
