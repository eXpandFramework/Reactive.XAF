function FilterLookup(editor, storageType, lookupViewId, cascadeMemberCaption,parentViewId) {
    var currentValue = editor.GetValue();
    var editorToFilter = GetEditor(parentViewId, lookupViewId);
    if (currentValue != null) {
        currentValue = currentValue.trim(")").substring(currentValue.indexOf("(") + 1, currentValue.indexOf(")"));
        PopulateLookup(editorToFilter, storageType, cascadeMemberCaption, currentValue);
    } else {
        PopulateLookup(editorToFilter, storageType);
    }
}

function GetEditor(parentViewId, lookupViewId) {
    let activeWindow = GetActiveWindow();
    var editorToFilter = activeWindow[parentViewId + "_LookupEditors"].find(function(x) {
        return eval(x).LookupViewId === lookupViewId;
    });
    return eval(editorToFilter);
}

function SynchronizeLookupValue(editor,lookupListViewId, parentViewId,  synchronizeMemberIndex, typeToSynchronize) {
    const selectedItem = editor.listBox.GetItem(editor.listBox.GetSelectedIndex());
    if (selectedItem != null) {
        if (editor.GetValue() !== null) {
            const currentValue = selectedItem.texts[synchronizeMemberIndex];
            let editorToSynch = GetEditor(parentViewId,lookupListViewId);
            editorToSynch.SetValue(typeToSynchronize + "(" + currentValue + ")");
        }
    }
}

function ArrayBufferToString(buffer) {
    const bufView = new Uint16Array(buffer);
    const length = bufView.length;
    var result = "";
    var addition = Math.pow(2, 16) - 1;

    for (let i = 0; i < length; i += addition) {
        if (i + addition > length) {
            addition = length - i;
        }

        result += String.fromCharCode.apply(null, bufView.subarray(i, i + addition));
    }

    return result;
}

function GetActiveWindow() {
    var p = typeof window.GetActivePopupControl === "function";
    if (p) {
        p = window.GetActivePopupControl();
    }
    if (!p)
        return window;
    else
        return p.GetContentIFrameWindow();
}


function GetDatasource(lookupViewId, storageType) {
    
    const item = eval(storageType)[lookupViewId];
    var strData = atob(item);
    const charData = strData.split("").map(function(x) {
        return x.charCodeAt(0);
    });
    const binData = new Uint8Array(charData);
    const data = window.pako.inflate(binData);
    strData = ArrayBufferToString(data);
    const objects = JSON.parse(strData).map(function(x) {
        return {
            Key: x.Key,
            Columns: x.Columns.split("&").map(function(y) {
                return decodeURIComponent(y.replace(/\+/g, "%20"));
            })
        };
    });
    return objects;
}

function PopulateLookup(editor, storageType, filterColumnCaption, filterValue) {
    var cbobjects = GetDatasource(editor.LookupViewId, storageType);

    if (cbobjects) {
        const filterColumnIndex = cbobjects[0].Columns.findIndex(function(x) {
            return x === filterColumnCaption;
        });
        editor.ClearItems();
        editor.BeginUpdate();
        for (let i = 1; i < cbobjects.length; i++) {
            const object = cbobjects[i];
            if (filterColumnIndex > -1) {
                if (object.Columns[filterColumnIndex] === filterValue) {
                    editor.AddItem(object.Columns, object.Key);
                }
            } else {
                editor.AddItem(object.Columns, object.Key);
            }
        }
        editor.EndUpdate();
    }
}


function EditorInit(editor, lookupViewId, parentViewId, storageType, currentText, currentValue) {
    let activeWindow = GetActiveWindow();
    activeWindow.SetEditorIsValid(editor);
    editor.LookupViewId = lookupViewId;
    let editors = activeWindow[parentViewId + "_LookupEditors"];
    
    if (!editors) {
        editors = new Array();
        window[parentViewId + "_LookupEditors"] = editors;
    }
    if (!editors.includes(editor.name)) {
        editors.push(editor.name);
    }

    PopulateLookup(editor, storageType);
    if (editors.length > 1 && currentValue) {
        eval(editors[0]).RaiseValueChangedEvent();
    }

    editor.BeginUpdate();
    editor.SetText(currentText);
    editor.SetValue(currentValue);
    editor.EndUpdate();
}

function ClearEditorItems(storageType, view) {
    let activeWindow = GetActiveWindow();
    let entries = Object.entries(activeWindow).filter(function(x) {
        if (view) {
            return x[0] === view + "_LookupEditors";
        }

        return x[0].endsWith("_LookupEditors");
    });

    var addCurrentItem = function(editor,value) {
        const cboObjects = GetDatasource(editor.LookupViewId, storageType);
        const find = cboObjects.find(function(x) {
            return x.Key === value;
        });
        editor.AddItem(find.Columns, find.Key);
        editor.SetSelectedIndex(0);
    };
    for (let j = 0; j < entries.length; j++) {
        let entry = entries[j];
        const lookupEditors = entry[1];

        for (let i = 0; i < lookupEditors.length; i++) {
            let editor;
            try {
                editor = eval(lookupEditors[i]);
                if (editor) {
                    editor.BeginUpdate();
                    var value = editor.GetValue();
                    editor.ClearItems();
                    if (value) {
                        addCurrentItem(editor,value);
                    }

                    editor.EndUpdate();
                }
            } catch (e) {
            }
        }
        activeWindow[entries[j][0]] = new Array();
    }
}

function RequestDatasources(handlerId, storageType) {
    const storage = eval(storageType);
    if (storage.LookupDataSources === undefined) {
        storage.setItem("LookupDataSources", true);
        window.RaiseXafCallback(window.globalCallbackControl, handlerId, "", "", false);
    }
}

function StoreDatasource(lookupViewId, json, storageType) {
    eval(storageType).setItem(lookupViewId, json);
}
