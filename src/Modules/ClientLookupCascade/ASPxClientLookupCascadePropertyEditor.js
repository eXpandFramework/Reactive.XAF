﻿
function PopulateLookup(uniqueId,storageType, filterColumnCaption, filterValue) {
    const cbobjects = GetDatasource(uniqueId,storageType);
    if (cbobjects) {
        const s = eval(uniqueId);
        LogTime(`ADDITEMS-${uniqueId} (${cbobjects.length})`,false);
        console.log("second object="+cbobjects[1].Columns);

        addItems(s,filterColumnCaption);
        LogTime(`ADDITEMS-${uniqueId} (${cbobjects.length})`, true);
    }

    function addItems(s,filterColumnCaption) {
        let filterColumnIndex = cbobjects[0].Columns.findIndex(x => x === filterColumnCaption);
        console.log("filterColumnIndex="+filterColumnIndex);
        s.ClearItems();
        s.BeginUpdate();
        for (let i = 1; i < cbobjects.length; i++) {
            const object = cbobjects[i];
            if (filterColumnIndex) {
                if (object.Columns[filterColumnIndex] === filterValue) {
                    s.AddItem(object.Columns, object.Key);
                }
            } else {
                s.AddItem(object.Columns, object.Key);
            }
        }
        s.EndUpdate();
    }
}

function FilterLookup(s, storageType, cascadeMember, cascadeMemberCaption) {
    console.log(`FilterLookup=${cascadeMember} in member `+cascadeMemberCaption);
    var currentValue = s.GetValue();
    if (currentValue != null) {
        currentValue = currentValue.trim(")").substring(currentValue.indexOf("(") + 1, currentValue.indexOf(")"));
        console.log("currentValue=" + currentValue);
        PopulateLookup(cascadeMember, storageType, cascadeMemberCaption, currentValue);
    } else {
        PopulateLookup(cascadeMember, storageType);
    }
}


function SynchronizeLookupValue(s,  uniqueId, synchronizeMember, synchronizeMemberCaption,typeToSynchronize) {
    console.log(`SynchronizeLooupValues-${uniqueId}`);
    let selectedItem = s.listBox.GetItem(s.listBox.GetSelectedIndex());
    if (selectedItem != null) {
        if (s.GetValue()!==null ) {
            let synchronizeMemberIndex = s.listBox.columnFieldNames.findIndex(x=>x === synchronizeMemberCaption);
            const currentValue = selectedItem.texts[synchronizeMemberIndex];
            console.log("currentValue="+currentValue);
            const synchronizedEditor = eval(synchronizeMember);
            console.log("synchronizeMember="+synchronizeMember);
            synchronizedEditor.SetValue(typeToSynchronize+"(" + currentValue + ")");
        }
    }
}

function ClearEditorItems(storageType,view) {
    
    const clearEditorItemsCore = function( id,storage) {
        try {
            console.log(`ClearEditorItems-${id}`);
            const editor = eval(id);
            if (editor !== undefined&& editor !==null) {
                editor.BeginUpdate();
                var value = editor.GetValue();
                console.log(`editorValue=${value}`);
                editor.ClearItems();
                if (value !== null) {
                    const cboObjects = GetDatasource(id,storage);
                    const find = cboObjects.find(x => x.Key === value);
                    console.log(`find=${find}`);
                    console.log(`findColumns=${find.Columns}`);
                    console.log(`findKey=${find.Key}`);
                    editor.AddItem(find.Columns, find.Key);
                    editor.SetSelectedIndex(0);

                }
                editor.EndUpdate();
            }
        } catch (e) {
        }
    };
    let entries = Object.entries(Window).filter(x => {
        if (view != undefined) {
            return x[0]===view+"_LookupEditors";
        }
        return x[0].endsWith("_LookupEditors");
    });
    for (var j = 0; j < entries.length; j++) {
        let entry = entries[j];
        let lookupEditors = entry[1];
        for (var i = 0; i < lookupEditors.length; i++) {
            let editor = lookupEditors[i];
            clearEditorItemsCore(editor, storageType);
        }
    }
    
    perfromanceTime = performance.now();
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

async function StoreDatasource(uniqueId, json,storageType) {
    LogTime(`StoreDataSource-`+uniqueId);
    eval(storageType).setItem(uniqueId, json);
    LogTime(`StoreDataSource-`+uniqueId, true);
}

function GetDatasource(uniqueId,storageType) {
    LogTime(`PARSE-${uniqueId}`);
    const item = GetStorageItem(eval(storageType), uniqueId);
    var strData     = atob(item);
    var charData    = strData.split('').map(function(x){return x.charCodeAt(0);});
    var binData     = new Uint8Array(charData);
    var data        = window.pako.inflate(binData);
    strData = ArrayBufferToString(data);
    const objects = JSON.parse(strData).map(x => {
        return {
            Key:x.Key,
            Columns:x.Columns.split('&').map(y=>decodeURIComponent(y.replace(/\+/g, '%20')))
        }
    });
    LogTime(`PARSE-${uniqueId}`, true);
    return objects;
}

function LogTime(text, end) {
    if (end === true) {
        console.timeEnd(`-----> ${text} <--------`);
    } else {
        console.time(`-----> ${text} <--------`);
    }
}

var perfromanceTime = null;

function RequestDatasources(handlerId,storageType) {
    let storage = eval(storageType);
    if (storage.LookupDataSources === undefined) {
        storage.setItem("LookupDataSources", true);
        window.RaiseXafCallback(window.globalCallbackControl, handlerId, "", "", false);
    }
}
function GetStorageItem(storage, editor) {
    let key = Object.keys(storage).find(x => x.startsWith(editor.substring(0, editor.indexOf("_guid_")-1)));
    return storage[key];
};

function EditorInit(s,e,uniqueId,parentViewId,storageType,currentText, currentValue) {
    window.SetEditorIsValid(s);
    if (!Window[parentViewId+'_LookupEditors'].includes(uniqueId)) {
        console.log('Pushing '+uniqueId);
        Window[parentViewId+'_LookupEditors'].push(uniqueId);
    }
    PopulateLookup(uniqueId, storageType);
    s.BeginUpdate();
    s.SetText(currentText);
    s.SetValue(currentValue);
    s.EndUpdate();
}