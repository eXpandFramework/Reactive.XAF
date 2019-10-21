/*		FileDataTestControl		*/
function FileDataTestControl(id, caption) {
	this.className = 'FileDataTestControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetIsAutoPostBackAction = function(name) {
		//this.error = null;
		this.LogEntry('GetIsAutoPostBackAction'); 	
		this.InitControl();	
		try {
			if(name == 'Change file') {
				return false;
			}
			else {
				return this.AutoPostBack;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetIsAutoPostBackAction');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.LogOperationError('It is impossible to set the value of fileinput field by JavaScript or control is in view mode');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var fileExists, isBrowseVisible, isViewMode, browseBlock, browseElement;
			if(this.control.cells[0].childNodes[0].cells[0].childNodes.length == 0)
			    fileExists = false;
			else 
			    fileExists = true;
			browseBlock = this.control.cells[1].childNodes[0];
			isViewMode = false;
			if(browseBlock == null || browseBlock == 'undefined' || browseBlock.childNodes.length == 0)
			{
			    if(this.control.cells[0].childNodes[0].cells[1].childNodes.length == 0)
			    {
			        isViewMode = true;
			        isBrowseVisible = false;
			    }
			    else
			    {
			        this.LogOperationError('Browse input not found');
			        return;
			    }
			}
			else
			{
			browseElement = browseBlock.childNodes[0];
			isBrowseVisible = browseBlock.style.display != 'none';
			}
			var result = '';
			if(fileExists)
			{
			    result = this.control.cells[0].childNodes[0].cells[0].childNodes[0].innerHTML;
			    if(!isViewMode) { 
			        if(isBrowseVisible)
			            result =  '(With actions & visible browse)' + result;
			        else
			            result =  '(With actions & invisible browse)' + result;
			    }
			}
			else if(isViewMode)
			    result = this.control.cells[1].innerHTML;
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			switch(value)
			{
			    case 'Clear':
					aspxBClick(this.id + '_Clear');
			    break;
			    case 'Change file':
					aspxBClick(this.id + '_Edit');
			    break;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		ASPxSchedulerRecurrenceInfoEdit		*/
function ASPxSchedulerRecurrenceInfoEdit(id, caption) {
	this.className = 'ASPxSchedulerRecurrenceInfoEdit';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.control.SetValue(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetValue();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.inputElement.readOnly;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(!value) {
				if(!this.control.clientEnabled){
					this.LogOperationError('Unable to execute actions in disabled control: ' + this.caption);					
				}
				if(this.control.GetButton(0).isDisabled){
					this.LogOperationError('Unable to execute Edit action in control: ' + this.caption);
				}else{
					aspxBEClick(this.control.name, 0);
				}		   
			}
			else {	
				if(!recurrencePopupControl.GetVisible()) { 
					this.LogOperationError('Unable to click ' + value + ' button if recurrence edit popup is not visible');					
				}
				var buttons = _aspxGetChildrenByPartialClassName(recurrenceInfoCallbackPanel.mainElement, 'dxbButton');
				for(var i = 0; i < buttons.length ; i++) {
					var clientASPxButton = eval(buttons[i].id.replace('_B', ''));
					if(value == clientASPxButton.GetText()) {
						clientASPxButton.DoClick();
						return;
					}
				}
				this.LogOperationError('Button ' + value + ' is not found for editor ' + this.caption);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ScrollControlTestControl		*/
function ScrollControlTestControl(id, caption) {
	this.className = 'ScrollControlTestControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var pos = value.split('_');
			scrollXPositionHolder.value = pos[0];
			scrollYPositionHolder.value = pos[1];				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return '' + document.documentElement.scrollLeft + '_' + document.documentElement.scrollTop;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var x = parseInt(scrollXPositionHolder.value);
			var y = parseInt(scrollYPositionHolder.value);
			window.scrollTo(x, y);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		JSASPxDropDownSingleChoiceActionControl		*/
function JSASPxDropDownSingleChoiceActionControl(id, caption) {
	this.className = 'JSASPxDropDownSingleChoiceActionControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.IsActionItemVisible = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemVisible'); 	
		this.InitControl();	
		try {
			var comboBox = this.control.GetComboBox();
			for(var i = 0; i < comboBox.GetItemCount(); i++) {
			    if(comboBox.GetItem(i).text == actionItemName) {
			        return true; 
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemVisible');
	}

	this.IsActionItemEnabled = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemEnabled'); 	
		this.InitControl();	
		try {
			return this.IsActionItemVisible(actionItemName);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemEnabled');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.control.SetText(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetText();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control) {
			    this.control.Act(value);
			}
			else {
			this.LogOperationError('The item ' + value + ' is not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		NavigationActionContainerControl		*/
function NavigationActionContainerControl(id, caption) {
	this.className = 'NavigationActionContainerControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.CollectCaptionFromNavBar = function(navBar) {
		//this.error = null;
		this.LogEntry('CollectCaptionFromNavBar'); 	
		this.InitControl();	
		try {
			var allCaptions = new Array();
			for( var i = 0; i < navBar.groups.length; i++) {
				var groupCaptions = new Array();
				this.AddCaptionToArray(groupCaptions, navBar.groups[i].name, navBar.groups[i].name);
				var truncatedCaption = caption;
				if(caption.indexOf('.')!=-1) {
					truncatedCaption = caption.substring(caption.indexOf('.') + 1, caption.length);
				}
				var treeId = navBar.cpGroupToTreeListMap ? navBar.cpGroupToTreeListMap[navBar.groups[i].name] : null;
				if(treeId){
					var tree = eval('window[\'' + treeId + '\']');
					var treeCaptions = this.CollectCaptionFromTreeList(tree, navBar.groups[i].name);
					this.MergeArrays(groupCaptions, treeCaptions);
				}							
				else {
					for(var j = 0; j < navBar.groups[i].items.length; j++) {
						var itemText = navBar.groups[i].items[j].GetText();
						this.AddCaptionToArray(groupCaptions, itemText, navBar.groups[i].name + '.' + itemText);
					}
				}
				this.MergeArrays(allCaptions, groupCaptions);
			}
			return allCaptions;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CollectCaptionFromNavBar');
	}

	this.CollectCaptionFromTreeList = function(tree, parentCaption) {
		//this.error = null;
		this.LogEntry('CollectCaptionFromTreeList'); 	
		this.InitControl();	
		try {
			var treeCaptions = new Array();
			for(var caption in tree.cpNodeCaptionToNodeKeyMap) {
				var keys = tree.cpNodeCaptionToNodeKeyMap[caption];
				for(var j = 0; j < tree.cpNodeCaptionToNodeKeyMap[caption].length; j++) {
					var fullCaption = tree.cpNodeNodeKeyToFullCaptionMap[tree.cpNodeCaptionToNodeKeyMap[caption][j]];
					if(parentCaption) {
					fullCaption = parentCaption + '.' + fullCaption;
					}
					this.AddCaptionToArray(treeCaptions, caption, fullCaption);
				}
			}
			return treeCaptions;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CollectCaptionFromTreeList');
	}

	this.AddCaptionToArray = function(array, caption, fullCaption) {
		//this.error = null;
		this.LogEntry('AddCaptionToArray'); 	
		this.InitControl();	
		try {
			var fullCaptionArray = array[caption];
			if(!fullCaptionArray) {
				array[caption] = new Array();
				fullCaptionArray = array[caption];
			}
			var hasFullCaption = false;
			for(var i = 0; i < fullCaptionArray.length; i++) {
				if(fullCaptionArray[i] == fullCaption) {
					hasFullCaption = true;
					break;
				}
			}
			if(!hasFullCaption) {
				fullCaptionArray[fullCaptionArray.length] = fullCaption;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('AddCaptionToArray');
	}

	this.MergeArrays = function(targetArray, array) {
		//this.error = null;
		this.LogEntry('MergeArrays'); 	
		this.InitControl();	
		try {
			for(var caption in array) {
				var fullCaptions = array[caption];
				for(var j = 0; j < fullCaptions.length; j++) {
					this.AddCaptionToArray(targetArray, caption, fullCaptions[j]);
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('MergeArrays');
	}

	this.GetFullPath = function(value) {
		//this.error = null;
		this.LogEntry('GetFullPath'); 	
		this.InitControl();	
		try {
			var caption = value;
			var aspxControl;				
			eval('aspxControl = window[\'' + this.id + '\']');
			var allCaptions;
			if(aspxControl.groups) {
				allCaptions = this.CollectCaptionFromNavBar(aspxControl);
			} else {
				allCaptions = this.CollectCaptionFromTreeList(aspxControl);
			}
			var captionFullNames = allCaptions[caption];
			if(captionFullNames && captionFullNames.length == 1) {
				return captionFullNames[0];
			} else {
				return caption;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetFullPath');
	}

	this.ExpandPath = function(value) {
		//this.error = null;
		this.LogEntry('ExpandPath'); 	
		this.InitControl();	
		try {
			var fullCaption = value;
			var aspxControl;				
			eval('aspxControl = window[\'' + this.id + '\']');
			var tree;
			if(!aspxControl.groups) {
				tree = aspxControl;
			} else {
				var groupName;
				if(fullCaption.indexOf('.')!=-1) {
					fullCaption = value.substring(value.indexOf('.') + 1, value.length);
					groupName = value.substring(0, value.indexOf('.'));
				}
				var treeId = aspxControl.cpGroupToTreeListMap ? aspxControl.cpGroupToTreeListMap[groupName] : null;
				if(treeId){
					tree = eval('window[\'' + treeId + '\']');
				}							
			}
			if(tree) {
				if(fullCaption.lastIndexOf('.') != -1) {
					fullCaption = fullCaption.substring(0, fullCaption.lastIndexOf('.'));
					tree.ExpandNode(tree.cpNodeFullCaptionToNodeKeyMap[fullCaption]);
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExpandPath');
	}

	this.MergeArrays = function(targetArray, array) {
		//this.error = null;
		this.LogEntry('MergeArrays'); 	
		this.InitControl();	
		try {
			for(var caption in array) {
				var fullCaptions = array[caption];
				for(var j = 0; j < fullCaptions.length; j++) {
					this.AddCaptionToArray(targetArray, caption, fullCaptions[j]);
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('MergeArrays');
	}

	this.FindAnchor = function(element) {
		//this.error = null;
		this.LogEntry('FindAnchor'); 	
		this.InitControl();	
		try {
			var result = null;
			if(element.tagName && element.tagName.toUpperCase() == 'A') {
				result = element;
			} else {
				for(var i = 0; i < element.children.length; i++) {
					result = this.FindAnchor(element.children[i])
					if(result) {
						break;
					}
				}
			}
			return result;					}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('FindAnchor');
	}

	this.ProcessFullCaptionByNavBar = function(navBar, caption) {
		//this.error = null;
		this.LogEntry('ProcessFullCaptionByNavBar'); 	
		this.InitControl();	
		try {
			var executableControl;
			var executed = false;
			for( var i = 0; i < navBar.groups.length;i++) {
			                      //this.error = null;
			                      if(executableControl) {
			                          break;
			                      }
				var truncatedCaption = caption;
				if(caption.indexOf('.')!=-1) {
					truncatedCaption = caption.substring(caption.indexOf('.') + 1, caption.length);
				}
				var treeId = navBar.cpGroupToTreeListMap ? navBar.cpGroupToTreeListMap[navBar.groups[i].name] : null;
				if(treeId){
					var tree = eval('window[\'' + treeId + '\']');
					if(this.ProcessFullCaptionByTree(tree, truncatedCaption)) {
						executed = true;
						break;
					}
				}							
				else {
					for( var j = 0; j < navBar.groups[i].items.length;j++) {
						if(navBar.groups[i].items[j].GetText() == truncatedCaption) {
								executableControl = navBar.groups[i].items[j];
								break;
						}
					}
				}
			}
			if(executableControl) {
				if(executableControl.navBar.DoItemClick != undefined && !executableControl.disabled) {
					executableControl.navBar.DoItemClick(executableControl.group.index, executableControl.index, false);
					executed = true;
				}
				else {
					this.LogOperationError('The "' + caption + '" item of the "' + this.caption + '" Action is disabled');
					return;
				}
			}
			if(!executed) {
				this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + caption + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ProcessFullCaptionByNavBar');
	}

	this.ProcessFullCaptionByTree = function(tree, fullCaption) {
		//this.error = null;
		this.LogEntry('ProcessFullCaptionByTree'); 	
		this.InitControl();	
		try {
			var node = tree.cpNodeFullCaptionToNodeKeyMap[fullCaption];
			if(node) {
				var canClick = tree.cpNodeKeyToInfoMap[node];
				if(canClick) {
					var nodeElement = tree.GetNodeHtmlElement(node);
					var anchor = this.FindAnchor(nodeElement);
					if(anchor) {
						anchor.click();
						return true;
					} else {
						return false;
					}
				} else {
					return false;
				}
			} else {
			                      //this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + fullCaption + '" item');
				return false;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ProcessFullCaptionByTree');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = window[\'' + this.id + '\']');
			var result;
			var allCaptions = new Array();
			var allCaptionCounter = 0;
			if(aspxControl.groups) {
			for( var i = 0; i < aspxControl.groups.length; i++) {
			        var treeId = aspxControl.cpGroupToTreeListMap ? aspxControl.cpGroupToTreeListMap[aspxControl.groups[i].name] : null;
			        if(treeId){
			var tree = eval('window[\'' + treeId + '\']');
			            var focusedNodeKey = tree.GetFocusedNodeKey();
			for(var caption in tree.cpNodeCaptionToNodeKeyMap) {
			                allCaptions[allCaptionCounter] = caption;
			                allCaptionCounter++;
			    var keys = tree.cpNodeCaptionToNodeKeyMap[caption];
			    for(var j = 0; j < tree.cpNodeCaptionToNodeKeyMap[caption].length; j++) {
			                    if(focusedNodeKey == tree.cpNodeCaptionToNodeKeyMap[caption][j]){
			                        result = caption;
			                    }
			                }
			            }
			}
			    }
			} else {
			var focusedNodeKey = aspxControl.GetFocusedNodeKey();
			//                    for(var caption in aspxControl.cpNodeCaptionToNodeKeyMap) {
			//                        allCaptions[allCaptionCounter] = caption;
			//                        allCaptionCounter++;
			//				        var keys = aspxControl.cpNodeCaptionToNodeKeyMap[caption];
			//				        for(var j = 0; j < aspxControl.cpNodeCaptionToNodeKeyMap[caption].length; j++) {
			//                            if(focusedNodeKey == aspxControl.cpNodeCaptionToNodeKeyMap[caption][j]){
			//                                result = caption;
			//                            }
			//                        }
			//                    }
			    result = aspxControl.cpNodeNodeKeyToFullCaptionMap[focusedNodeKey];
			}
			var duplicatedCount = 0;
			for(var i =0; i < allCaptions.length; i++){
			    if(allCaptions[i] == result){
			        duplicatedCount++;
			    }
			}
			if(duplicatedCount > 1){
			if(aspxControl.groups) {
			allCaptions = this.CollectCaptionFromNavBar(aspxControl);
			} else {
			allCaptions = this.CollectCaptionFromTreeList(aspxControl);
			}
			    var captionFullNames = allCaptions[caption];
			if(captionFullNames && captionFullNames.length > 1) {
			this.LogOperationError('The "' + this.caption + '" action contains several items with the "' + caption + '" caption. (' + captionFullNames.join(", ") + ')');
			return;
			}
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var caption = value;
			var aspxControl;				
			eval('aspxControl = window[\'' + this.id + '\']');
			var allCaptions;
			if(aspxControl.groups) {
				allCaptions = this.CollectCaptionFromNavBar(aspxControl);
			} else {
				allCaptions = this.CollectCaptionFromTreeList(aspxControl);
			}
			var captionFullNames = allCaptions[caption];
			if(captionFullNames && captionFullNames.length > 1) {
				this.LogOperationError('The "' + this.caption + '" action contains several items with the "' + caption + '" caption. (' + captionFullNames.join(", ") + ')');
				return;
			} else {
				var fullCaption = (captionFullNames && captionFullNames.length == 1) ? captionFullNames[0] : caption;
				if(aspxControl.groups) {
					this.ProcessFullCaptionByNavBar(aspxControl, fullCaption);
				} else {
					this.ProcessFullCaptionByTree(aspxControl, fullCaption);
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		NavigationHistoryActionContainer		*/
function NavigationHistoryActionContainer(id, caption) {
	this.className = 'NavigationHistoryActionContainer';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetItem = function(value) {
		//this.error = null;
		this.LogEntry('GetItem'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			var i;
			            var executableControl = null;
			for(i = 0; i < aspxControl.children.length; i++) {
			 if(aspxControl.children[i].innerText == value) {
			  executableControl = aspxControl.children[i];
			  break;
			 }
			}
			            return executableControl;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetItem');
	}

	this.IsActionItemVisible = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemVisible'); 	
		this.InitControl();	
		try {
			var item = this.GetItem(actionItemName);
			return item != null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemVisible');
	}

	this.IsActionItemEnabled = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemEnabled'); 	
		this.InitControl();	
		try {
			var item = this.GetItem(actionItemName);
			if(item) {
			    if(item.click != 'undefined' && !item.disabled && item.href != '') {
			        return true;
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemEnabled');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			var i, result = '';
			for(i = 0; i < aspxControl.children.length; i++) {
				result += aspxControl.children[i].innerText;
				if(i != aspxControl.children.length - 1) {
					result += ';';
				}
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var item = this.GetItem(value);
			if(item) {
			if(this.IsActionItemEnabled(value)) {
			item.click();
			}
			else {
			this.LogOperationError('The "' + value + '" item of the "' + this.caption + '" Action is disabled');
			}
			}
			else {
			this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + value + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		NavigationLinksActionContainer		*/
function NavigationLinksActionContainer(id, caption) {
	this.className = 'NavigationLinksActionContainer';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetItem = function(value) {
		//this.error = null;
		this.LogEntry('GetItem'); 	
		this.InitControl();	
		try {
			var div = this.control.children[0];
			var items = null;
			if (div.children[0].innerHTML) {
			    items = div.children[0];
			}
			else {
			    items = div.children[1];
			}
			items = items.children[0].children[0].children[0].children[0].children[0].children[0].children;
			for(var i = 0; i < items.length; i++) {
			if(items[i].children[0].innerText == value) {
			        return items[i];
			}
			}
			return null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetItem');
	}

	this.IsActionItemVisible = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemVisible'); 	
		this.InitControl();	
		try {
			var item = this.GetItem(actionItemName);
			return item != null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemVisible');
	}

	this.IsActionItemEnabled = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemEnabled'); 	
		this.InitControl();	
		try {
			var item = this.GetItem(actionItemName);
			if(item) {
			    if(item.children[0].getAttribute('href')) {
			        return true;
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemEnabled');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var item = this.GetItem(value);
			if(item) {
			if(this.IsActionItemEnabled(value)) {
			item.click();
			}
			else {
			this.LogOperationError('The "' + value + '" item of the "' + this.caption + '" Action is disabled');
			}
			}
			else {
			this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + value + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		ASPxTreeSingleChoiceActionControl		*/
function ASPxTreeSingleChoiceActionControl(id, caption) {
	this.className = 'ASPxTreeSingleChoiceActionControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetHint = function() {
		//this.error = null;
		this.LogEntry('GetHint'); 	
		this.InitControl();	
		try {
			if(this.control) {
			    return this.control.GetMainButton().mainElement.title;
			}
			return '';				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetHint');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control) {
			    this.control.Act(value);
			}
			else {
			this.LogOperationError('The item ' + value + ' is not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxTextBox		*/
function ASPxTextBox(id, caption, autoPostBack) {
	this.className = 'ASPxTextBox';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			//B34542
			if(this.control.inputElement.maxLength < value.length){
				this.LogOperationError('The maximum number of characters allowed by the editor is ' + this.control.inputElement.maxLength + '. You\'ve tried to input ' + value.length + ' characters.');
				return;
			}

			if(this.control.inputElement.readOnly) {
			this.LogOperationError('The "' + this.caption + '" editor is readonly.');
			return;
			}

			this.control.SetValue(value);

			if(this.autoPostBack) {
			   xafDoPostBack(this.control.name, '');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var value = this.control.GetValue();			
			return (value == null) ? '' : value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			hasMainElementMethod = false;
			isMainElementEnabled = false;
			if(this.control.GetMainElement) {
				hasMainElementMethod = true;
				isMainElementEnabled = !this.control.GetMainElement().isDisabled;
			}
			hasGetEnabledMethod = false;
			isEnabled = false;
			if(this.control.GetEnabled) {
				hasGetEnabledMethod = true;
				isEnabled = this.control.GetEnabled();
			}
			//B150245
			if((hasGetEnabledMethod && hasMainElementMethod) && (isEnabled && !isMainElementEnabled)){
				return isMainElementEnabled;
			}
			if(hasGetEnabledMethod) {
				return isEnabled;
			}
			if(this.control.enabled != true && this.control.enabled != false) {
				if(this.control.GetInputElement) {
					return !this.control.GetInputElement().isDisabled;
				} else {
					return false;
				}
			}
			return this.control.enabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxDate		*/
function ASPxDate(id, caption, autoPostBack) {
	this.className = 'ASPxDate';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(this.control.inputElement.readOnly) {
				this.LogOperationError('The "' + this.caption + '" editor is readonly.');
				return;
			}			

				this.control.SetText(value);
				aspxETextChanged(this.control.name);
			if(this.autoPostBack) {
			    xafDoPostBack(this.control.name, '');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var value = this.control.GetFormattedDate();
			return (value == null) ? '' : value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			hasMainElementMethod = false;
			isMainElementEnabled = false;
			if(this.control.GetMainElement) {
				hasMainElementMethod = true;
				isMainElementEnabled = !this.control.GetMainElement().isDisabled;
			}
			hasGetEnabledMethod = false;
			isEnabled = false;
			if(this.control.GetEnabled) {
				hasGetEnabledMethod = true;
				isEnabled = this.control.GetEnabled();
			}
			//B150245
			if((hasGetEnabledMethod && hasMainElementMethod) && (isEnabled && !isMainElementEnabled)){
				return isMainElementEnabled;
			}
			if(hasGetEnabledMethod) {
				return isEnabled;
			}
			if(this.control.enabled != true && this.control.enabled != false) {
				if(this.control.GetInputElement) {
					return !this.control.GetInputElement().isDisabled;
				} else {
					return false;
				}
			}
			return this.control.enabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control.inputElement.readOnly) {
				this.LogOperationError('The "' + this.caption + '" editor is readonly.');
				return;
			}			
					if(value == 'Clear'	){
								this.control.SetText('');
								aspxETextChanged(this.control.name);
							}
			if(this.autoPostBack) {
			    xafDoPostBack(this.control.name, '');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxSpin		*/
function ASPxSpin(id, caption, autoPostBack) {
	this.className = 'ASPxSpin';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(this.control.inputElement.readOnly) {
				this.LogOperationError('The "' + this.caption + '" editor is readonly.');
				return;
			}			

			this.control.SetValue(value);

			if(this.autoPostBack) {
			    xafDoPostBack(this.control.name, '');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			//TODO Bykov 
			// replace next line with 'var value = this.control.GetText();'
			var value = this.control.GetFormattedNumber(this.control.GetNumber());
			return (value == null) ? '' : '' + value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			hasMainElementMethod = false;
			isMainElementEnabled = false;
			if(this.control.GetMainElement) {
				hasMainElementMethod = true;
				isMainElementEnabled = !this.control.GetMainElement().isDisabled;
			}
			hasGetEnabledMethod = false;
			isEnabled = false;
			if(this.control.GetEnabled) {
				hasGetEnabledMethod = true;
				isEnabled = this.control.GetEnabled();
			}
			//B150245
			if((hasGetEnabledMethod && hasMainElementMethod) && (isEnabled && !isMainElementEnabled)){
				return isMainElementEnabled;
			}
			if(hasGetEnabledMethod) {
				return isEnabled;
			}
			if(this.control.enabled != true && this.control.enabled != false) {
				if(this.control.GetInputElement) {
					return !this.control.GetInputElement().isDisabled;
				} else {
					return false;
				}
			}
			return this.control.enabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxComboBox		*/
function ASPxComboBox(id, caption, autoPostBack) {
	this.className = 'ASPxComboBox';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var isFound = false;					
			for(i = 0; i < this.control.GetItemCount(); i++) {
				if(this.control.GetItem(i).text == value) {
					this.control.SetSelectedIndex(i);
					aspxEValueChanged(this.control.name);
					isFound = true;
					break;
				}
			}
			if (!isFound) {
				if(this.control.isDropDownListStyle) {
				this.LogOperationError('Cannot change the ' + this.caption + ' control\'s value. The list of available values doesn\'t contain the specified value');
				} else {

			if(this.control.inputElement.readOnly) {
			this.LogOperationError('The "' + this.caption + '" editor is readonly.');
			return;
			}

			this.control.SetValue(value);

			if(this.autoPostBack) {
			 xafDoPostBack(this.control.name, '');
			}

				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetText();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			this.SetText(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxCheckBox		*/
function ASPxCheckBox(id, caption, autoPostBack) {
	this.className = 'ASPxCheckBox';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var preparedValue = value.toLowerCase();
			if(preparedValue != 'true' && preparedValue != 'false') {
			 this.LogOperationError('A Boolean property can only be set to True or False. The "' + value + '" value is invalid.');
			                return;
			}
			this.control.SetChecked(eval(preparedValue));				
			this.control.RaiseCheckedChanged();				
			if(this.control.autoPostBack) {					
			 this.control.SendPostBackInternal('');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetChecked() ? 'True' : 'False';				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			hasMainElementMethod = false;
			isMainElementEnabled = false;
			if(this.control.GetMainElement) {
				hasMainElementMethod = true;
				isMainElementEnabled = !this.control.GetMainElement().isDisabled;
			}
			hasGetEnabledMethod = false;
			isEnabled = false;
			if(this.control.GetEnabled) {
				hasGetEnabledMethod = true;
				isEnabled = this.control.GetEnabled();
			}
			//B150245
			if((hasGetEnabledMethod && hasMainElementMethod) && (isEnabled && !isMainElementEnabled)){
				return isMainElementEnabled;
			}
			if(hasGetEnabledMethod) {
				return isEnabled;
			}
			if(this.control.enabled != true && this.control.enabled != false) {
				if(this.control.GetInputElement) {
					return !this.control.GetInputElement().isDisabled;
				} else {
					return false;
				}
			}
			return this.control.enabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxRadioButtonList		*/
function ASPxRadioButtonList(id, caption) {
	this.className = 'ASPxRadioButtonList';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(value == 'True'){
				aspxChkOnClick(this.control.name);
			}
			else {
				this.LogError('It is impossible to set false value to radio button');
			}							}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetValue();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxSimpleLookup		*/
function ASPxSimpleLookup(id, caption) {
	this.className = 'ASPxSimpleLookup';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.InitListBox = function() {
		//this.error = null;
		this.LogEntry('InitListBox'); 	
		this.InitControl();	
		try {
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
			    return;
			}
			if(eval('window.'+this.id + '_DropDown')) {
			    this.listBox = eval('new ASPxComboBox(this.id + \'_DropDown\', this.caption)');
			}
			else {
			    this.listBox = this.control;
			}
			this.listBox.targetErrorControl = this;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitListBox');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			//var listBox = eval('new ASPxComboBox(this.id + \'_DropDown\', this.caption)');
			//this.listBox.targetErrorControl = this;
			            this.InitListBox();
			this.listBox.SetText(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			//var listBox = eval('new ASPxComboBox(this.id + \'_DropDown\', this.caption)');
			//this.listBox.targetErrorControl = this;
			            this.InitListBox();
			return this.listBox.GetText();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			var dropDown = eval(this.control.id + '_DropDown');
			if(dropDown) {
			    return dropDown.enabled;
			}
			else {
			    return this.control.enabled;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(value == undefined || value == null)	{
			 this.LogOperationError('The default Action is not defined for the ' + this.caption + ' control');
			 return;
			}
			            row = this.control.rows[0];
			var button = null;
			for(var i = 0; i < row.cells.length; i++) {
			 for(var j = 0; j < row.cells[i].childNodes.length; j++) {
			  var el = row.cells[i].childNodes[j];
			  if(!el.id) 
			   continue;
			  button = this.GetClientControlById(el.id);
			  if(button == this.control || !button || !button.GetMainElement)
			   continue;
			  if(button.GetMainElement().title == value) {
			   button.DoClick();
			   return;
			  }
			 }
			}
			this.LogOperationError('The "' + value + '" Action is not defined for the ' + this.caption + ' control');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxMemo		*/
function ASPxMemo(id, caption, autoPostBack) {
	this.className = 'ASPxMemo';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(this.control.inputElement.readOnly) {
				this.LogOperationError('The "' + this.caption + '" editor is readonly.');
				return;
			}			

			this.control.SetValue(value);

			if(this.autoPostBack) {
			    xafDoPostBack(this.control.name, '');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var value = this.control.GetValue();			
			return (value == null) ? '' : value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			hasMainElementMethod = false;
			isMainElementEnabled = false;
			if(this.control.GetMainElement) {
				hasMainElementMethod = true;
				isMainElementEnabled = !this.control.GetMainElement().isDisabled;
			}
			hasGetEnabledMethod = false;
			isEnabled = false;
			if(this.control.GetEnabled) {
				hasGetEnabledMethod = true;
				isEnabled = this.control.GetEnabled();
			}
			//B150245
			if((hasGetEnabledMethod && hasMainElementMethod) && (isEnabled && !isMainElementEnabled)){
				return isMainElementEnabled;
			}
			if(hasGetEnabledMethod) {
				return isEnabled;
			}
			if(this.control.enabled != true && this.control.enabled != false) {
				if(this.control.GetInputElement) {
					return !this.control.GetInputElement().isDisabled;
				} else {
					return false;
				}
			}
			return this.control.enabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxLookupPropertyEditor		*/
function ASPxLookupPropertyEditor(id, caption) {
	this.className = 'ASPxLookupPropertyEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetEditControl = function() {
		//this.error = null;
		this.LogEntry('GetEditControl'); 	
		this.InitControl();	
		try {
			if(this.editControl == null){
			                   if(eval('window.'+this.id + '_Edit')) {
			                       this.editControl = eval('new ASPxTextBox(this.id + \'_Edit\', this.caption)');
			                   }
			                   else {
								for(var j = 0; j < this.control.rows[0].cells[0].childNodes.length; j++) {
									var el = this.control.rows[0].cells[0].childNodes[j];
			 					this.editControl = this.GetClientControlById(el.id);
			  				if(this.editControl) {
			   				break;
			    		}
			                       }
			                   }
			               }
			               this.editControl.targetErrorControl = this;
			return this.editControl;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetEditControl');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.GetEditControl().SetText(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.GetEditControl().GetText();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.GetEditControl().IsEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(!value) {
				value = 'Find';
			}
			var row = this.control.rows[0];
			var button = null;
			for(var i = 0; i < row.cells.length; i++) {
				for(var j = 0; j < row.cells[i].childNodes.length; j++) {
					var el = row.cells[i].childNodes[j];
					button = this.GetClientControlById(el.id);
					if(!button || !button.GetMainElement)
						continue;
					if(button.GetMainElement().title == value) {
						button.DoClick();
						return;
					}
				}
			}
			this.LogOperationError('Unrecognized editor Action: ' + value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxButtonEdit		*/
function ASPxButtonEdit(id, caption) {
	this.className = 'ASPxButtonEdit';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetValue();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.GetButton(0).isDisabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control.GetButton(0).isDisabled){
				this.LogOperationError('Cannot execute the Edit Action for the ' + this.caption + ' control');
			}else{
				aspxBEClick(this.control.name, 0);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		JSNavigationTabsTestControlOld		*/
function JSNavigationTabsTestControlOld(id, caption) {
	this.className = 'JSNavigationTabsTestControlOld';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			var control = this.control;
			var actionContainers = document.getElementById(this.id + '_CC');
			var itemIndex = -1;
			var itemCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			for(var i = 0; i < itemCount; i++) {
				var item = document.getElementById(this.id + '_T' + i);
				if(item.innerText.indexOf(value) != -1) {
					itemIndex = i;
					break;
				}						
				item = document.getElementById(this.id + '_AT' + i);
				if(item.innerText.indexOf(value) != -1) {
					itemIndex = i;
					break;
				}						
			}
			if(itemIndex != -1) {
				aspxControl.SetActiveTab(aspxControl.GetTab(itemIndex));
			}
			else {
				this.LogOperationError('The "' + value + '" navigation item does not exist');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			var control = this.control;
			var actionContainers = document.getElementById(this.id + '_CC');
			var activeItem = null;
			var itemCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			for(var i = 0; i < itemCount; i++) {
				var item = document.getElementById(this.id + '_T' + i);
				if(item.style.display != 'none' && (item.click == 'undefined' || item.disabled || item.innerHTML.indexOf('TabRightPartSelected') != -1)) {
					activeItem = item;
					break;
				}						
				item = document.getElementById(this.id + '_AT' + i);
				if(item.style.display != 'none' && (item.click == 'undefined' || item.disabled || item.innerHTML.indexOf('TabRightPartSelected') != -1)) {
					activeItem = item;
					break;
				}						
			}
			if(activeItem) {
				return activeItem.innerText;
			}
			else {
				return "There is no any active navigation item";
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var aspxControl;
			eval('aspxControl = ' + this.id);
			var control = this.control;
			var actionContainers = document.getElementById(this.id + '_CC');
			var executableControl = null;
			var groupCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			var itemCount = groupCount;
			for(var i = 0; i < groupCount && executableControl == null; i++){
				var navGroupActionContainer = document.getElementById(this.id + '_C' + i);
				if(navGroupActionContainer) {
					for(var j = 0; j < navGroupActionContainer.childNodes.length; j += 2) {
						if(navGroupActionContainer.childNodes[j].innerText == value) {
							executableControl = navGroupActionContainer.childNodes[j];
							break;
						}
					}
				}
			}
			if(!executableControl) {
				for(var i = 0; i < itemCount; i++) {
					var item = document.getElementById(this.id + '_T' + i);
					if(!item) {
						item = document.getElementById(this.id + '_AT' + i);
					}
					if(item.innerText == value) {
						executableControl = item;
						if(!executableControl.onclick) {
							executableControl.disabled = true;
						}
						break;
					}						
				}
			}
			if(executableControl) {
				if(executableControl.click != 'undefined' && !executableControl.disabled) {
					executableControl.click();
				}
				else {
					this.LogOperationError('The "' + value + '" item of the "' + this.caption + '" Action is disabled');
				}
			}
			else {
				this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + value + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		JSTabsTestControl		*/
function JSTabsTestControl(id, caption) {
	this.className = 'JSTabsTestControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetExecutableControl = function(value) {
		//this.error = null;
		this.LogEntry('GetExecutableControl'); 	
		this.InitControl();	
		try {
			var aspxControl;
			            var pageControlId = this.id + '_PageControl';
			eval('aspxControl = ' + pageControlId);
			var control = this.control;
			var actionContainers = document.getElementById(pageControlId+ '_CC');
			var executableControl = null;
			var groupCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			var itemCount = groupCount;
			            var executableControlCaption = value;
			            var tabCaption;

			if(value.indexOf('.')!=-1) {
			                tabCaption = value.substring(0, value.indexOf('.'));
			    executableControlCaption = value.substring(value.indexOf('.') + 1, value.length);
			            }
			for(var i = 0; i < groupCount && executableControl == null; i++){
			                var navGroupActionContainerId = this.id + '_PageControl_Menu' + i + '_Menu';
			                this.navGroupActionContainer = null;
			                if(document.getElementById(navGroupActionContainerId)) {
			                    this.navGroupActionContainer = eval(navGroupActionContainerId);
			                }
			                var tabGroup;
			                if(tabCaption) {
			                   tabGroup = document.getElementById(this.id + '_T' + i);
			                }
			 if(this.navGroupActionContainer) {
			                    var tabGroupCaption;
			                    if(tabGroup){
			                        tabGroupCaption = tabGroup.innerText;
			                    }   
			                    if(tabCaption && tabGroupCaption && tabCaption != tabGroupCaption){
			                            continue;
			                    }
			  for(var j = 0; j < this.navGroupActionContainer.GetItemCount(); j ++) {
			                        var item = this.navGroupActionContainer.GetItem(j);
			   if(item.GetText() == executableControlCaption) {
			    executableControl = item;
			    break;
			   }
			  }
			 }
			}
			if(!executableControl) {
			 for(var i = 0; i < itemCount; i++) {
			  var item = document.getElementById(pageControlId + '_T' + i);
			                    var active = false;
			  if(!item) {
			   item = document.getElementById(pageControlId + '_AT' + i);
			                        active = true;
			  }
			  if(item.innerText == value) {
			   executableControl = item;
			                        executableControl.disabled = !active && !executableControl.onclick;
			                        executableControl.GetEnabled = function() {
			                            return !this.disabled;
			                        }
			                        executableControl.GetVisible = function() {
			                            return true;
			                        }
			   break;
			  }						
			 }
			}
			            return executableControl;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetExecutableControl');
	}

	this.IsActionItemVisible = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemVisible'); 	
		this.InitControl();	
		try {
			var executableControl = this.GetExecutableControl(actionItemName);
			if(executableControl) {
			    return executableControl.GetVisible();
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemVisible');
	}

	this.IsActionItemEnabled = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemEnabled'); 	
		this.InitControl();	
		try {
			var executableControl = this.GetExecutableControl(actionItemName);
			if(executableControl) {
			    return executableControl.GetEnabled();
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemEnabled');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			var control = this.control;
			var actionContainers = document.getElementById(this.id + '_CC');
			var itemIndex = -1;
			var itemCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			for(var i = 0; i < itemCount; i++) {
			 var item = document.getElementById(this.id + '_T' + i);
			 if(item.innerText.indexOf(value) != -1) {
			  itemIndex = i;
			  break;
			 }						
			 item = document.getElementById(this.id + '_AT' + i);
			 if(item.innerText.indexOf(value) != -1) {
			  itemIndex = i;
			  break;
			 }						
			}
			if(itemIndex != -1) {
			 aspxControl.ChangeActiveTab(itemIndex, false);
			}
			else {
			 this.LogOperationError('The "' + value + '" navigation item does not exist');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			            var result = aspxControl.GetTabText(aspxControl.activeTabIndex);
			            if(result && result != '') {
			                return result;
			            }
			            else {
			                this.LogOperationError('There is now active layout tab or it\'s text is empty.');
			            }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var executableControl = this.GetExecutableControl(value);
			if(executableControl) {
			    if(executableControl.GetEnabled()) {
			        if(this.navGroupActionContainer) {
			            this.navGroupActionContainer.DoItemClick(executableControl.GetIndexPath(), false, null);
			        }
			        else {
			            if(executableControl.onclick) {
			                executableControl.click();
			            }
			        }
			    }
			else {
			this.LogOperationError('The "' + value + '" item of the "' + this.caption + '" Action is disabled');
			}
			}
			else {
			this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + value + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		JSNavigationTabsTestControl		*/
function JSNavigationTabsTestControl(id, caption) {
	this.className = 'JSNavigationTabsTestControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetExecutableControl = function(value) {
		//this.error = null;
		this.LogEntry('GetExecutableControl'); 	
		this.InitControl();	
		try {
			var aspxControl;
			            var pageControlId = this.id + '_PageControl';
			eval('aspxControl = ' + pageControlId);
			var control = this.control;
			var actionContainers = document.getElementById(pageControlId+ '_CC');
			var executableControl = null;
			var groupCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			var itemCount = groupCount;
			            var executableControlCaption = value;
			            var tabCaption;

			if(value.indexOf('.')!=-1) {
			                tabCaption = value.substring(0, value.indexOf('.'));
			    executableControlCaption = value.substring(value.indexOf('.') + 1, value.length);
			            }
			for(var i = 0; i < groupCount && executableControl == null; i++){
			                var navGroupActionContainerId = this.id + '_PageControl_Menu' + i + '_Menu';
			                this.navGroupActionContainer = null;
			                if(document.getElementById(navGroupActionContainerId)) {
			                    this.navGroupActionContainer = eval(navGroupActionContainerId);
			                }
			                var tabGroup;
			                if(tabCaption) {
			                   tabGroup = document.getElementById(this.id + '_T' + i);
			                }
			 if(this.navGroupActionContainer) {
			                    var tabGroupCaption;
			                    if(tabGroup){
			                        tabGroupCaption = tabGroup.innerText;
			                    }   
			                    if(tabCaption && tabGroupCaption && tabCaption != tabGroupCaption){
			                            continue;
			                    }
			  for(var j = 0; j < this.navGroupActionContainer.GetItemCount(); j ++) {
			                        var item = this.navGroupActionContainer.GetItem(j);
			   if(item.GetText() == executableControlCaption) {
			    executableControl = item;
			    break;
			   }
			  }
			 }
			}
			if(!executableControl) {
			 for(var i = 0; i < itemCount; i++) {
			  var item = document.getElementById(pageControlId + '_T' + i);
			                    var active = false;
			  if(!item) {
			   item = document.getElementById(pageControlId + '_AT' + i);
			                        active = true;
			  }
			  if(item.innerText == value) {
			   executableControl = item;
			                        executableControl.disabled = !active && !executableControl.onclick;
			                        executableControl.GetEnabled = function() {
			                            return !this.disabled;
			                        }
			                        executableControl.GetVisible = function() {
			                            return true;
			                        }
			   break;
			  }						
			 }
			}
			            return executableControl;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetExecutableControl');
	}

	this.IsActionItemVisible = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemVisible'); 	
		this.InitControl();	
		try {
			var executableControl = this.GetExecutableControl(actionItemName);
			if(executableControl) {
			    return executableControl.GetVisible();
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemVisible');
	}

	this.IsActionItemEnabled = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemEnabled'); 	
		this.InitControl();	
		try {
			var executableControl = this.GetExecutableControl(actionItemName);
			if(executableControl) {
			    return executableControl.GetEnabled();
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemEnabled');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var aspxControl;				
			eval('aspxControl = ' + this.id);
			var control = this.control;
			var actionContainers = document.getElementById(this.id + '_CC');
			var itemIndex = -1;
			var itemCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			for(var i = 0; i < itemCount; i++) {
			 var item = document.getElementById(this.id + '_T' + i);
			 if(item.innerText.indexOf(value) != -1) {
			  itemIndex = i;
			  break;
			 }						
			 item = document.getElementById(this.id + '_AT' + i);
			 if(item.innerText.indexOf(value) != -1) {
			  itemIndex = i;
			  break;
			 }						
			}
			if(itemIndex != -1) {
			 aspxControl.ChangeActiveTab(itemIndex, false);
			}
			else {
			 this.LogOperationError('The "' + value + '" navigation item does not exist');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var aspxControl;
			var pageControlId = this.id + '_PageControl';
			aspxControl = eval(pageControlId);
			var result = aspxControl.GetTabText(aspxControl.activeTabIndex);
			var activeTabMenuControl = eval(this.id + '_PageControl_Menu' + aspxControl.activeTabIndex + '_Menu');
			var selectedItem = activeTabMenuControl.GetSelectedItem();
			if(selectedItem) {
			    result += '.' + selectedItem.GetText();
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var executableControl = this.GetExecutableControl(value);
			if(executableControl) {
			    if(executableControl.GetEnabled()) {
			        if(this.navGroupActionContainer) {
			            this.navGroupActionContainer.DoItemClick(executableControl.GetIndexPath(), false, null);
			        }
			        else {
			            if(executableControl.onclick) {
			                executableControl.click();
			            }
			        }
			    }
			else {
			this.LogOperationError('The "' + value + '" item of the "' + this.caption + '" Action is disabled');
			}
			}
			else {
			this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + value + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		ASPxGridListEditor		*/
function ASPxGridListEditor(id, caption) {
	this.className = 'ASPxGridListEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.InlineEdit = function(param, rowIndex, columnIndex) {
		//this.error = null;
		this.LogEntry('InlineEdit'); 	
		this.InitControl();	
		try {
			var clientGridView = this.GetClientGridView(rowIndex);
			               if(clientGridView) {
			                   if(rowIndex == -1) {
			                       if(!clientGridView.GetEditingRow(clientGridView))
			            this.LogOperationError('The grid editing row is undefined');
			                   } else {
			                       if(this.IsCommandAvailable(clientGridView, clientGridView.GetTopVisibleIndex() + rowIndex, 'Edit')) {
			                  clientGridView.StartEditRow(clientGridView.GetTopVisibleIndex() + rowIndex);
			                       }
			                       else {
			                           if(clientGridView.IsEditing()) {
			                   clientGridView.StartEditRow(clientGridView.GetTopVisibleIndex() + rowIndex);
			                           } else {
			                   this.LogOperationError('The \'Edit\' Action is not available');
			                           }
			                       }
			                   }
			               }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InlineEdit');
	}

	this.InlineUpdate = function(param, rowIndex, columnIndex) {
		//this.error = null;
		this.LogEntry('InlineUpdate'); 	
		this.InitControl();	
		try {
			var clientGridView = this.GetClientGridView(rowIndex);
			               if(clientGridView){
			                   if(this.IsCommandAvailable(clientGridView, rowIndex, 'Update')) {
			     clientGridView.UpdateEdit();
			                   }
			                   else {
			  this.LogOperationError('The \'Update\' Action is not available.');
			                   }
			               }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InlineUpdate');
	}

	this.InlineCancel = function(param, rowIndex, columnIndex) {
		//this.error = null;
		this.LogEntry('InlineCancel'); 	
		this.InitControl();	
		try {
			var clientGridView = this.GetClientGridView(rowIndex);
			               if(clientGridView){
			                   if(this.IsCommandAvailable(clientGridView, clientGridView.GetTopVisibleIndex() + rowIndex, 'Cancel')) {
			     clientGridView.CancelEdit();
			                   }
			                   else {
			  this.LogOperationError('The \'Cancel\' Action is not available.');
			                   }
			               }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InlineCancel');
	}

	this.InlineNew = function() {
		//this.error = null;
		this.LogEntry('InlineNew'); 	
		this.InitControl();	
		try {
			var clientGridView = this.GetClientGridView(-1);
			               if(clientGridView){
			                   if(this.IsCommandAvailable(clientGridView, 0, 'New')) {
			     clientGridView.AddNewRow();
			                   }
			                   else {
			  this.LogOperationError('The \'New\' Action is not available.');
			                   }
			               }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InlineNew');
	}

	this.SortByColumn = function(columnCaption) {
		//this.error = null;
		this.LogEntry('SortByColumn'); 	
		this.InitControl();	
		try {
			var columnFound;
			var clientGridView = eval(this.control.id);
			var testColumns = eval(this.control.id + '_TestColumns');
			for(var i = 0; i < clientGridView.columns.length; i++) {
			    var column = clientGridView.columns[i];
			    var fieldName = testColumns.GetColumnFieldNameByColumnCaption(columnCaption);
			    if(fieldName == '') {
			        this.LogOperationError( 'The \''+ columnCaption + '\' column was not found.');
			    }
			    if(column.fieldName == fieldName) {
			        if(this.GetColumnIndex(columnCaption) != -1) {
			            clientGridView.SortBy(column);
			        }
			        else {
			            this.LogOperationError( 'The \''+ columnCaption + '\' column was found, but it is invisible.');
			        }
			        columnFound = true;
			    }
			}                                
			if(!columnFound) {
			    this.LogOperationError( 'The grid does not contain the ' + columnCaption + ' column.');
			}										                    				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SortByColumn');
	}

	this.SelectAll = function(checked) {
		//this.error = null;
		this.LogEntry('SelectAll'); 	
		this.InitControl();	
		try {
			if(!checked) {
			    checked = 'true';
			}
			var selectAllCheckBox = eval(eval(this.control.id+'_SelectAllCheckBoxId'));
			selectAllCheckBox.SetChecked(eval(checked.toLowerCase()));
			selectAllCheckBox.RaiseCheckedChanged();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SelectAll');
	}

	this.SetPageSize = function(size) {
		//this.error = null;
		this.LogEntry('SetPageSize'); 	
		this.InitControl();	
		try {
			var comboBox = new ASPxComboBox(eval(this.control.id+'_PageSizeChooserComboBoxId'), 'PageSize');
			comboBox.targetErrorControl = this;
			comboBox.SetText(size);
			//var pageSizeChooserComboBox = eval(eval(this.control.id+'_PageSizeChooserComboBoxId'));
			//pageSizeChooserComboBox.SetValue(size);
			//pageSizeChooserComboBox.RaiseSelectedIndexChanged();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetPageSize');
	}

	this.SetPage = function(pageIndex) {
		//this.error = null;
		this.LogEntry('SetPage'); 	
		this.InitControl();	
		try {
			pageIndex--;
			if(pageIndex>= 0) {
			    var clientGridView = eval(this.control.id);
			    clientGridView.GotoPage(pageIndex);	
			}
			else {
			    this.LogOperationError('Page index must greater than 1.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetPage');
	}

	this.ExpandGroup = function(groupRowText) {
		//this.error = null;
		this.LogEntry('ExpandGroup'); 	
		this.InitControl();	
		try {
			this.ProcessGroup(groupRowText, 0);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExpandGroup');
	}

	this.CollapseGroup = function(groupRowText) {
		//this.error = null;
		this.LogEntry('CollapseGroup'); 	
		this.InitControl();	
		try {
			this.ProcessGroup(groupRowText, 1);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CollapseGroup');
	}

	this.GetClientGridView = function(rowIndex) {
		//this.error = null;
		this.LogEntry('GetClientGridView'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			if(!clientGridView) this.LogOperationError('It is impossible to perform action. The grid is empty');
			var rowCount = this.GetTableRowCount();
			if(rowIndex != -1 && rowIndex > rowCount - 1) {
			    this.LogOperationError('The grid contains ' + rowCount + ' rows');
			return '';
			}
			return clientGridView;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientGridView');
	}

	this.InitControl = function() {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
			 return;
			}
			if(this.control.tagName.toUpperCase() != 'TABLE') {
			 this.control = null;
			} else {
			 this.control = this.control;
			}
			if(this.control && this.control.id) {
			 eval(eval(this.control.id).cpTestColumns);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetCheckBoxStateOrImageTitle = function(cell) {
		//this.error = null;
		this.LogEntry('GetCheckBoxStateOrImageTitle'); 	
		this.InitControl();	
		try {
			if(cell && cell.tagName && (cell.tagName.toUpperCase() == 'IMG' || (cell.tagName.toUpperCase() == 'INPUT' && cell.type && cell.type == 'checkbox'))) {
			 if(cell.tagName.toUpperCase() == 'IMG' && !IsNull(cell.title)) {
			  return cell.title != '' ? cell.title : cell.alt;
			 }
			 if(cell.tagName.toUpperCase() == 'INPUT' && cell.type == 'checkbox') {
			  return cell.checked ? 'True' : 'False';
			 }
			}
			else {
			 for(var i=0;i<cell.childNodes.length;i++){
			  var result = this.GetCheckBoxStateOrImageTitle(cell.childNodes[i]);
			  if(result) {
			   return result;
			  }
			 }
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetCheckBoxStateOrImageTitle');
	}

	this.GetActionColumnCount = function() {
		//this.error = null;
		this.LogEntry('GetActionColumnCount'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			var count = 0;
			if(clientGridView) {

			 //Bug in clientGridView.GetColumnsCount()
			 //var columnCount = clientGridView.GetColumnsCount();

			 var headersRow = document.getElementById(this.control.id + '_DXHeadersRow');
			 var columnCount = headersRow.childNodes.length;

			 for(var i = 0; i < columnCount; i++) {
			  var column = clientGridView.GetColumn(clientGridView._getNumberFromEndOfString(headersRow.childNodes[i].id));
			  if(!column || !column.fieldName || column.fieldName == '') {
			   count++;
			  }
			 }
			}
			return count;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetActionColumnCount');
	}

	this.GetGroupColumnCount = function() {
		//this.error = null;
		this.LogEntry('GetGroupColumnCount'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			var count = 0;
			if(clientGridView) {
			    var headersRow = document.getElementById(this.control.id + '_DXHeadersRow');
			    var columnCount = headersRow.childNodes.length;
			    for(var i = 0; i < columnCount; i++) {
			     var column = clientGridView.GetColumn(clientGridView._getNumberFromEndOfString(headersRow.childNodes[i].id));
			     if(!column) {
			      count++;
			     }
			    }
			}
			return count;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetGroupColumnCount');
	}

	this.IsCommandAvailable = function(clientGridView, rowIndex, commandName) {
		//this.error = null;
		this.LogEntry('IsCommandAvailable'); 	
		this.InitControl();	
		try {
			var dataRow = null;
			if(rowIndex != -1) {
			    dataRow = clientGridView.GetDataRow(rowIndex);
			}
			if(!dataRow) {
			    if(clientGridView.cpInlineEditMode == 'EditFormAndDisplayRow' || clientGridView.cpInlineEditMode == 'EditForm') {
			        var editFormTable = eval(this.id+'_DXEFT');
			        dataRow = editFormTable.rows[editFormTable.rows.length-1];
			    }
			    else {
			        dataRow = clientGridView.GetEditingRow(clientGridView);
			    }
			}
			if(!dataRow) dataRow = clientGridView.GetEditingRow(clientGridView);
			for(var i = 0; i < dataRow.cells.length; i++) {
			    var dataRowCell = dataRow.cells[i];
			    if(!dataRowCell.className) continue;
			    if(dataRowCell.className.indexOf('dxgvCommandColumn') != 0) continue;
			    for(var j = 0; j < dataRowCell.childNodes.length; j++) {
			     var currentColumnCommandName = dataRowCell.childNodes[j].innerText;
			     if(!currentColumnCommandName || currentColumnCommandName == '') {
			      currentColumnCommandName = dataRowCell.childNodes[j].alt;
			     }
			     if(currentColumnCommandName == commandName) 
			      return true;
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsCommandAvailable');
	}

	this.FilterColumn = function(columnCaption, filterCriteria) {
		//this.error = null;
		this.LogEntry('FilterColumn'); 	
		this.InitControl();	
		try {
			var testColumns = eval(this.control.id + '_TestColumns');
			var columnIndex = testColumns.GetColumnIndexByColumnCaption(columnCaption);
			if(columnIndex!=-1){
			    var clientGridView = eval(this.control.id);
			    clientGridView.ApplyHeaderFilterByColumn(columnIndex, filterCriteria);
			}
			else {
			    this.LogOperationError( 'The grid does not contain the ' + columnCaption + ' column.');
			}										                    				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('FilterColumn');
	}

	this.FindActionColumnIndex = function(row, actionName) {
		//this.error = null;
		this.LogEntry('FindActionColumnIndex'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			var actionColumnCount = this.GetActionColumnCount();
			for(var i = 0; i < actionColumnCount; i++) {
			    var actionColumnIndex = i - actionColumnCount + 1;
			    var cellValue = this.GetCellValue(row, actionColumnIndex);
			    if(cellValue == actionName) {
			     return i;
			    }
			}
			return -1;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('FindActionColumnIndex');
	}

	this.SetRowSelection = function(row, isSelected) {
		//this.error = null;
		this.LogEntry('SetRowSelection'); 	
		this.InitControl();	
		try {
			var rowCount = this.GetTableRowCount();
			if(row > rowCount - 1) {
			    this.LogOperationError( 'The grid contains ' + rowCount + ' rows');
			    return;
			}			
			var clientGridView = eval(this.control.id);
			clientGridView.SelectRow(clientGridView.GetTopVisibleIndex() + row, isSelected);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetRowSelection');
	}

	this.ProcessGroup = function(groupRowText, actionIndex) {
		//this.error = null;
		this.LogEntry('ProcessGroup'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			var i = 0;
			var foundedGroupRows = [];
			while(clientGridView.GetRow(i)) {
			    var groupRow = clientGridView.GetGroupRow(i++);
			    if(groupRow) {
			        var preparedInnerText ='';
			        for(var j=0; j<=groupRow.childNodes.length;j++) {
			            preparedInnerText  += this.Trim(groupRow.childNodes[1].childNodes[0].childNodes[0].childNodes[0].childNodes[j].innerText) + ' ';
			        }
			        preparedInnerText = this.Trim(preparedInnerText);
			        //var preparedInnerText = this.RemoveLineBrakes(this.Trim(groupRow.innerText));
			        foundedGroupRows.push(preparedInnerText);
			     if(preparedInnerText == groupRowText) {
			            switch(actionIndex) {
			                case 0: clientGridView.ExpandRow(i - 1); break;
			                case 1: clientGridView.CollapseRow(i - 1); break;
			            }		                                
			      return;
			     }
			    }
			}
			this.LogOperationError( 'The grid doesn\'t contain the \'' + groupRowText + '\'' + ' group row. Found:\r\n' + foundedGroupRows.join('\r\n') + '\r\n');
			return;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ProcessGroup');
	}

	this.Trim = function(str) {
		//this.error = null;
		this.LogEntry('Trim'); 	
		this.InitControl();	
		try {
			while (str.substring(0,1) == ' ') {
			    str = str.substring(1, str.length);
			}
			while (str.substring(str.length-1, str.length) == ' ') {
			    str = str.substring(0,str.length-1);
			}
			return str;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Trim');
	}

	this.RemoveLineBrakes = function(str) {
		//this.error = null;
		this.LogEntry('RemoveLineBrakes'); 	
		this.InitControl();	
		try {
			str = str.replace(/\r/g,'');
			str = str.replace(/\n/g,'');
			str = str.replace(/<BR>/g,'');
			return str;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('RemoveLineBrakes');
	}

	this.ColumnEditorAction = function(columnCaption, columnAction) {
		//this.error = null;
		this.LogEntry('ColumnEditorAction'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			if(!clientGridView) {
			    this.LogOperationError('Cannot perform an Action. The grid doesn\'t contain any records');
			    return;
			}
			var rowElement = clientGridView.GetEditingRow(clientGridView);
			if(!rowElement) {
			    this.LogOperationError('Cannot perform an Action. The edit row is disabled.');
			    return;
			}
			var columnIndex = this.GetColumnIndex(columnCaption);
			if(columnIndex == -1) {
			    this.LogOperationError('Cannot find the ' + columnCaption + ' column.');
			    return;
			}
			var testControl = this.GetColumnEditor(rowElement.sectionRowIndex - 1, columnIndex);
			if(testControl) {
			    testControl.Act(columnAction)
			    this.CheckEditorError(testControl);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ColumnEditorAction');
	}

	this.GetColumnEditor = function(rowIndex, columnIndex) {
		//this.error = null;
		this.LogEntry('GetColumnEditor'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			var testControl = this.GetColumnEditorCore(-2147483647, columnIndex - this.GetGroupColumnCount());
			if(testControl) {
			return testControl;
			}
			else {
			return this.GetColumnEditorCore(clientGridView.GetTopVisibleIndex() + rowIndex, columnIndex - this.GetGroupColumnCount());
			}
		}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnEditor');
	}

	this.GetColumnEditorCore = function(rowIndex, columnIndex) {
		//this.error = null;
		this.LogEntry('GetColumnEditorCore'); 	
		this.InitControl();	
		try {
			try {
			    var testColumns = eval(this.control.id + '_TestColumns');
			    var testColumn = testColumns.FindColumnByIndexInGrid(this.GetActionColumnCount() + columnIndex);
			    if(testColumn) {
			        var editorId;
			        editorId = eval(this.control.id+'.cp' + testColumn.fieldName.replace('.', '_') + testColumn.indexInGrid + '_RowIndexToEditorIdMap')[rowIndex];
			        var testControl = testColumn.CreateTestControlWithId(editorId);
			        if(testColumn.editorTestClassNameEditMode == 'ASPxSimpleLookup') {
			         testControl.GetText();
			         if(testControl.error != null) {
			          var findLookupColumn = new TestColumn(testColumn.fieldName, 'ASPxLookupPropertyEditor', testColumn.editorId.replace('$DropDown', ''), testColumn.indexInGrid);
			          var findLookupTestControl = findLookupColumn.CreateTestControlWithId(findLookupColumn.editorId);
			          findLookupTestControl.GetText();
			          if(findLookupTestControl.error == null) {
			           testControl = findLookupTestControl;
			          }
			         }
			        }
			        return testControl;
			    }
			    this.LogOperationError('Cannot find the editor for the ' + (this.GetActionColumnCount() + columnIndex) + ' column.');
			} 
			catch(e) { }
			return null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnEditorCore');
	}

	this.CheckEditorError = function(testControl) {
		//this.error = null;
		this.LogEntry('CheckEditorError'); 	
		this.InitControl();	
		try {
			if(testControl.error != null) {
			 this.LogOperationError('Error in the editor for the ' + testControl.caption + ' property. Error message: ' + testControl.error);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CheckEditorError');
	}

	this.GetTableRowCount = function() {
		//this.error = null;
		this.LogEntry('GetTableRowCount'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			return clientGridView.GetVisibleRowsOnPage();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetTableRowCount');
	}

	this.IsSupportSelection = function() {
		//this.error = null;
		this.LogEntry('IsSupportSelection'); 	
		this.InitControl();	
		try {
			var selectAllCheckBox = eval(eval(this.control.id+'_SelectAllCheckBoxId'));
			return selectAllCheckBox != null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsSupportSelection');
	}

	this.ClearSelection = function() {
		//this.error = null;
		this.LogEntry('ClearSelection'); 	
		this.InitControl();	
		try {
			this.SelectAll('false');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ClearSelection');
	}

	this.ExecuteAction = function(actionName, row, column) {
		//this.error = null;
		this.LogEntry('ExecuteAction'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			if(!clientGridView) this.LogOperationError('It is impossible to perform action. The grid is empty');
			var rowCount = this.GetTableRowCount();
			if(row > rowCount - 1) {
				this.LogOperationError( 'The grid contains ' + rowCount + ' rows');
				return '';
			}
			if(actionName != '') {
				var isFound = false;
				var columnActionIndex = this.FindActionColumnIndex(row, actionName);
				if(columnActionIndex != -1) {
					var actionCell = clientGridView.GetDataRow(row).cells[columnActionIndex + 1];
					for(i = 0; i < actionCell.childNodes.length; i++) {
						var innerElement = actionCell.childNodes[i];
						if(innerElement.tagName.toUpperCase() == 'A' && innerElement.innerText == actionName) {
							innerElement.click();
							break;
						}
					}
					actionCell.click();
				}
				else {
					var actionControl = TestControls.FindControl(1, this.caption + '.' + actionName);
					var isRootListView = clientGridView.GetMainElement().id == 'Grid';
					if(!actionControl && isRootListView) {
						actionControl = TestControls.FindControl(1, actionName);
					}
					if(!actionControl) {
					   this.LogOperationError('The ' + actionName + ' Action is not found in the "' + this.Caption + '" table\'s ' + row + ' row');
					} else {
						if(!actionControl.IsEnabled()){
							this.LogOperationError('The ' + actionControl.caption + ' Action is disabled');
							return;
						}
						actionControl.Act();
					}
				}
			//	this.SelectRow(row, false);
			}
			else {
				if(clientGridView.GetDataRow(row).getAttribute('IsClickDisabled') != 'true') {
					clientGridView.GetDataRow(row).click();
				} else {
					this.LogOperationError('The Default Action is disabled');
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExecuteAction');
	}

	this.SelectRow = function(row) {
		//this.error = null;
		this.LogEntry('SelectRow'); 	
		this.InitControl();	
		try {
			this.SetRowSelection(row, true);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SelectRow');
	}

	this.UnselectRow = function(row) {
		//this.error = null;
		this.LogEntry('UnselectRow'); 	
		this.InitControl();	
		try {
			this.SetRowSelection(row, false);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('UnselectRow');
	}

	this.IsRowSelected = function(row) {
		//this.error = null;
		this.LogEntry('IsRowSelected'); 	
		this.InitControl();	
		try {
			var rowCount = this.GetTableRowCount();
			if(row > rowCount - 1) {
				this.LogOperationError( 'The grid contains ' + rowCount + ' rows');
				return '';
			}			
			var clientGridView = eval(this.control.id);
			return clientGridView._isRowSelected(clientGridView.GetTopVisibleIndex() + row)				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsRowSelected');
	}

	this.GetCellValue = function(row, column) {
		//this.error = null;
		this.LogEntry('GetCellValue'); 	
		this.InitControl();	
		try {
			var result = '';
			var clientGridView = eval(this.control.id);
			if(clientGridView) {
			 var rowCount = this.GetTableRowCount();
			 if(row > rowCount - 1) {
			  this.LogOperationError( 'The grid contains ' + rowCount + ' rows');
			  return result;
			 }
			 var rowElement = clientGridView.GetEditingRow(clientGridView);
			 var isCellFound = false;
			 if(rowElement && (row == (rowElement.sectionRowIndex - 1))) {
			  var testControl = this.GetColumnEditor(rowElement.sectionRowIndex - 1, column);		
			  if(testControl) {
			   result = testControl.GetText();
			   isCellFound = true;
			   this.CheckEditorError(testControl);
			  }
			//		                    else {
			//			                    this.error = null;
			//			                    this.operationError = false;
			//		                    }
			 }
			 else {
			  rowElement = clientGridView.GetDataRow(clientGridView.GetTopVisibleIndex() + row);
			 }
			 if(rowElement && !isCellFound) {
			        var editor = this.GetColumnEditor(clientGridView.GetTopVisibleIndex() + row, column);
			        if(editor) {
			            result = editor.GetText();
			        }
			        else {
			            this.LogTraceMessage('Column ' + column + ' editor was not found. Gettign text from cell.');
			            var cell = rowElement.cells[this.GetActionColumnCount() + column];
			      var innerText = this.RemoveLineBrakes(this.Trim(cell.innerText));
			      if(cell.innerText.replace(' ', '') == '' && cell.childNodes.length > 0) {
			       result = this.GetCheckBoxStateOrImageTitle(cell.childNodes[0]);
			       if(!result) result = '';
			      }			
			      else {
			       result = innerText;
			       if(result == ' ') result = '';
			      }
			        }
			 }
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetCellValue');
	}

	this.GetColumnIndex = function(columnCaption) {
		//this.error = null;
		this.LogEntry('GetColumnIndex'); 	
		this.InitControl();	
		try {
			var testColumns = eval(this.control.id + '_TestColumns');
			return testColumns.GetColumnIndexByColumnCaption(columnCaption) + this.GetGroupColumnCount();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnIndex');
	}

	this.GetColumnsCaptions = function() {
		//this.error = null;
		this.LogEntry('GetColumnsCaptions'); 	
		this.InitControl();	
		try {
			var result = '';
			var clientGridView = eval(this.control.id);
			var actionColumnCount = this.GetActionColumnCount();
			var headersRow = clientGridView.GetHeadersRow();
			for(var i = 0; i < headersRow.childNodes.length - actionColumnCount; i++) {
			 var caption = headersRow.childNodes[i + actionColumnCount].innerText;
			 if(result == '') {
			  result = caption;
			 } 
			 else {
			  result += ';' + caption;
			 }
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnsCaptions');
	}

	this.BeginEdit = function(row) {
		//this.error = null;
		this.LogEntry('BeginEdit'); 	
		this.InitControl();	
		try {
			this.InlineEdit(null, row, null);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('BeginEdit');
	}

	this.GetCellControl = function(row, column) {
		//this.error = null;
		this.LogEntry('GetCellControl'); 	
		this.InitControl();	
		try {
			var result = '';
			var clientGridView = eval(this.control.id);
			if(clientGridView) {
				rowElement = clientGridView.GetEditingRow(clientGridView);
				if(rowElement) {
			                       var rowIndex =rowElement.sectionRowIndex - 1;	
			                       if(clientGridView.cpInlineEditMode == 'EditFormAndDisplayRow') {
			                           rowIndex = rowIndex - 1;
			                       }
					var testControl = this.GetColumnEditor(rowIndex, column);		
					return testControl;
				}
				else {
					this.LogOperationError('All rows are read-only.');
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetCellControl');
	}

	this.EndEdit = function() {
		//this.error = null;
		this.LogEntry('EndEdit'); 	
		this.InitControl();	
		try {
			this.InlineUpdate(null, -1, null);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('EndEdit');
	}

	this.SetCellValue = function(row, column, value) {
		//this.error = null;
		this.LogEntry('SetCellValue'); 	
		this.InitControl();	
		try {
			var result = '';
			var clientGridView = eval(this.control.id);
			if(clientGridView) {
			 var rowCount = this.GetTableRowCount();
			 if(row > rowCount - 1) {
			  this.LogOperationError( 'The grid contains ' + rowCount + ' rows');
			  return result;
			 }			
			 rowElement = clientGridView.GetEditingRow(clientGridView);
			 if(rowElement) {			
			  var testControl = this.GetColumnEditor(rowElement.sectionRowIndex - 1, column);		
			  if(testControl) {
			   testControl.SetText(value);
			   isCellFound = true;
			   this.CheckEditorError(testControl);
			  }
			 }
			 else {
			  this.LogOperationError('All the rows are read-only');
			 }
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetCellValue');
	}

	this.ExpandGroups = function() {
		//this.error = null;
		this.LogEntry('ExpandGroups'); 	
		this.InitControl();	
		try {
			var clientGridView = eval(this.control.id);
			var rowCount = clientGridView.GetVisibleRowsOnPage();
			//B38638
			for(var i = 0; i < rowCount; i++) {
				if(clientGridView.IsGroupRow(i)) {
					clientGridView.ExpandAll();
					return;
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExpandGroups');
	}
}
/*		ASPxHtmlPropertyEditor		*/
function ASPxHtmlPropertyEditor(id, caption) {
	this.className = 'ASPxHtmlPropertyEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.control.SetHtml(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var value = this.control.GetHtml();
			return (value == null) ? '' : value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled; 				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxMenu		*/
function ASPxMenu(id, caption) {
	this.className = 'ASPxMenu';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetItem(0).GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var selectedItem = null;
			if(value) {
				var path = value.split('.');
				if(path.length == 0) {
					this.LogOperationError('Item is not specified.');
					return;
				}
				var currentItem = null;						
				var startIndex = 0;
				if(this.control.GetItemCount() == 1 && this.control.GetItem(0).GetText() != path[0]) {
					currentItem = this.control.GetItem(0);
				}
				//New action
				if(this.control.GetItemCount() == 2) {							
					var title = '';
					if(this.caption.lastIndexOf('.') != -1) {
						title = this.caption.substr(this.caption.lastIndexOf('.') + 1);
					}
					else {
						title = this.caption;
					}
					if((this.control.GetItem(0).GetText() == title) && (this.control.GetItem(0).GetItemCount() == 0) && (this.control.GetItem(1).GetText() == '')) {
						currentItem = this.control.GetItem(1);
					}
				}
				if(!currentItem) {
					for(var j = 0; j < this.control.GetItemCount(); j++) {
						if(this.control.GetItem(j).GetText() == path[0]) {
							currentItem = this.control.GetItem(j);
							startIndex++;
							break;
						}
					}	
				}
				if(currentItem) {
					var nextLevel = true;
					for(var i = startIndex; i < path.length && nextLevel; i++) {
						nextLevel = false;
						for(var j = 0; j < currentItem.GetItemCount(); j++) {
							if(currentItem.GetItem(j).GetText() == path[i]) {
								currentItem = currentItem.GetItem(j);
								nextLevel = true;
								break;
							}
						}
					}
					if(nextLevel) {
						selectedItem = currentItem;
					}
				}
			}
			else {
				selectedItem = this.control.GetItem(0);
			}
			if(selectedItem) {
				if(selectedItem.GetEnabled()) {
					this.control.DoItemClick(selectedItem.GetIndexPath(), false, null);
				}
				else {
					this.LogOperationError('The "' + value + '" item is disabled');
				}
			}
			else {
				this.LogOperationError('The "' + value + '" item is not found');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxMenuParametrizedActionItem		*/
function ASPxMenuParametrizedActionItem(id, caption) {
	this.className = 'ASPxMenuParametrizedActionItem';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control) {
			    //TODO B30856
			    //if(this.button.GetEnabled()) {
			        this.control.SetText(value);
			this.control.DoClick();
			    //}
			    //else {
			    //    this.LogOperationError('The "' + value + '" item is Disabled.');	
			    //}
			}
			else {
			this.LogOperationError('The "' + value + '" item is not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		JSASPxTestParametrizedActionControl		*/
function JSASPxTestParametrizedActionControl(id, caption, menuId, itemName) {
	this.className = 'JSASPxTestParametrizedActionControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.menuId = menuId;
	this.itemName = itemName;
	this.GetHint = function() {
		//this.error = null;
		this.LogEntry('GetHint'); 	
		this.InitControl();	
		try {
			if(this.control) {
			    var button = eval(this.id+ '_Button');
			    return button.mainElement.title;
			}
			return '';				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetHint');
	}

	this.IsVisible = function() {
		//this.error = null;
		this.LogEntry('IsVisible'); 	
		this.InitControl();	
		try {
			var menu = eval(menuId);
			if(menu) {
			    var menuItem = menu.GetItemByName(itemName);
			    if(menuItem) {
			        return menuItem.GetVisible();
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsVisible');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var editor = eval(this.id+ '_Editor');
			editor.SetText(value);
			if(editor.ParseValueCore){
			    editor.ParseValueCore(true);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var editor = eval(this.id+ '_Editor');
			return editor.GetText();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			var editor = eval(this.id+ '_Editor');
			return editor.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control) {
			if(value != undefined) {
			this.SetText(value);
			}
			    var button = eval(this.id+ '_Button');
			    button.DoClick();
			}
			else {
			this.LogOperationError('The item ' + value + ' is not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxMenuHolder		*/
function ASPxMenuHolder(id, caption, actionName, actionCaption) {
	this.className = 'ASPxMenuHolder';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.actionName = actionName;
	this.actionCaption = actionCaption;
	this.GetHint = function() {
		//this.error = null;
		this.LogEntry('GetHint'); 	
		this.InitControl();	
		try {
			var actionName = this.actionName;
			               var currentItem = null;
			for(var j = 0; j < this.control.GetItemCount(); j++) {
				if(this.control.GetItem(j).name == actionName) {
					return this.control.GetItem(j).menu.GetItemTextCell(j).title;					
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetHint');
	}

	this.GetAction = function() {
		//this.error = null;
		this.LogEntry('GetAction'); 	
		this.InitControl();	
		try {
			var actionName = this.actionName;
			               var currentItem = null;
			for(var j = 0; j < this.control.GetItemCount(); j++) {
				if(this.control.GetItem(j).name == actionName) {
					currentItem = this.control.GetItem(j);
					break;
				}
			}
			               return currentItem;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetAction');
	}

	this.GetActionItem = function(action, actionItemCaption) {
		//this.error = null;
		this.LogEntry('GetActionItem'); 	
		this.InitControl();	
		try {
			if(action) {
			                   var currentItem = null;
			                   if(actionItemCaption && action.GetItemCount() > 0) {
			                       var targetItem = null;
			                       var itemsArray = actionItemCaption.split('.');
			                       currentItem = action;
			                       for(var j = 0; j<itemsArray.length; j++) {
			                           for(var i = 0; i<currentItem.GetItemCount(); i++) {
			                               if(currentItem.GetItem(i).GetText() == itemsArray[j]) {
			                                   targetItem = currentItem.GetItem(i);
			                                   currentItem = targetItem;
			                                   break;
			                               }
			                           }  
			                       }
			                       //B152604
			                       currentItem = targetItem;
			                   }
			                   //TODO B30856
			                   if(currentItem && currentItem != action){
			                       return currentItem;
			                   }
			}
						return null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetActionItem');
	}

	this.IsActionItemVisible = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemVisible'); 	
		this.InitControl();	
		try {
			var action = this.GetAction();
			if(action) {
			    var actionItem = this.GetActionItem(action, actionItemName);
			    if(actionItem) {
			        return actionItem.GetVisible();
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemVisible');
	}

	this.IsVisible = function() {
		//this.error = null;
		this.LogEntry('IsVisible'); 	
		this.InitControl();	
		try {
			var action = this.GetAction();
			if(action) {
			    return action.GetVisible();
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsVisible');
	}

	this.IsActionItemEnabled = function(actionItemName) {
		//this.error = null;
		this.LogEntry('IsActionItemEnabled'); 	
		this.InitControl();	
		try {
			var action = this.GetAction();
			if(action) {
			    var actionItem = this.GetActionItem(action, actionItemName);
			    if(actionItem) {
			        return actionItem.GetEnabled();
			    }
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsActionItemEnabled');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			var actionName = this.actionName;
			               var currentItem = null;
			for(var j = 0; j < this.control.GetItemCount(); j++) {
				if(this.control.GetItem(j).name == actionName) {
					currentItem = this.control.GetItem(j);							
					break;
				}
			}					
			if(currentItem) {
			                   return currentItem.GetEnabled();
			               }
			               else {
				this.LogOperationError('The "' + actionCaption + '" item is not found.');						
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var action = this.GetAction();
			if(action) {
			    var currentItem = action;
			    var actionItem = this.GetActionItem(action, value);
			    if(actionItem) {
			        currentItem = actionItem;
			    }
			    if(currentItem){
			        if(currentItem.GetVisible()) {
			            if(currentItem.GetEnabled()) {
			this.control.DoItemClick(currentItem.GetIndexPath(), false, null);
			            }
			            else {
			                this.LogOperationError('The "' + value + '" item is Disabled.');	
			            }
			        }
			        else {
			            this.LogOperationError('The "' + value + '" item is not Invisible.');	
			        }
			    }
			    else{
			         this.LogOperationError('The "' + value + '" item is not found.');
			    }
			}
			else {
			this.LogOperationError('The "' + value + '" item is not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		SchedulerEditor		*/
function SchedulerEditor(id, caption) {
	this.className = 'SchedulerEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.InitControl = function() {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			this.startOnColumnIndex = 0;
			this.endOnColumnIndex = 1;
			this.subjectColumnIndex = 2;
			this.dateTimeFormatString = 'MM/dd/yyyy H:mm';
			this.dateFormatString = 'MM/dd/yyyy';
			this.formatter = new ASPxDateFormatter();
			this.formatter.SetFormatString(this.dateTimeFormatString);
			eval('this.control = window.' + this.id.replace('$', '_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}									}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetMenuItemElement = function(menuItemName) {
		//this.error = null;
		this.LogEntry('GetMenuItemElement'); 	
		this.InitControl();	
		try {
			var namePath = menuItemName.split('.');
			var menuSuffixes = new Array('_aptMenuBlock_SMAPT', '_viewMenuBlock_SMVIEW');
			for(var s = 0; s < menuSuffixes.length; s++) {
				var menuId = this.id + menuSuffixes[s];
				var menu = eval(menuId);
				if(menu) {
					var indexesPath;
					var parentMenuItem = null;
					for(var i = 0; i < menu.GetItemCount(); i++) {
						if(menu.GetItemText(i) == namePath[0]) {
							parentMenuItem = menu.GetItem(i);
							indexesPath = i.toString();
							break;
						}
					}
					if(parentMenuItem) {
						var lastItemFound = true;
						for(var i = 1; i < namePath.length && lastItemFound; i++) {
							lastItemFound = false;
							for(var k = 0; k < parentMenuItem.GetItemCount() && !lastItemFound; k++) {
								if(parentMenuItem.GetItem(k).GetText() == namePath[i]) {
									parentMenuItem = parentMenuItem.GetItem(k);
									indexesPath += __aspxItemIndexSeparator + k.toString();
									lastItemFound = true;
								}
							}
						}
						if(lastItemFound) {
							var menuItem = menu.GetItemByIndexPath(indexesPath);
							if(!menuItem.GetEnabled()) {
								this.LogOperationError('The ' + menuItemName + ' item is not available.');
							}
							return menu.GetItemElement(indexesPath);
						}
					}
				}
			}
			return null;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetMenuItemElement');
	}

	this.GetAppointmentAt = function(row) {
		//this.error = null;
		this.LogEntry('GetAppointmentAt'); 	
		this.InitControl();	
		try {
			var appointmentsArray = null;
			var correctedRowNum = row;
			if(row < this.control.horizontalViewInfo.appointmentViewInfos.length) {
				appointmentsArray = this.control.horizontalViewInfo.appointmentViewInfos;
			}
			else {
				appointmentsArray = this.control.verticalViewInfo.appointmentViewInfos;
				correctedRowNum = row - this.control.horizontalViewInfo.appointmentViewInfos.length;
			}
			return appointmentsArray[correctedRowNum];				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetAppointmentAt');
	}

	this.FindActionColumnIndex = function(row, actionName) {
		//this.error = null;
		this.LogEntry('FindActionColumnIndex'); 	
		this.InitControl();	
		try {
			var menuItemElement = this.GetMenuItemElement(actionName);
			return menuItemElement ? menuItemElement.rowIndex : -1;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('FindActionColumnIndex');
	}

	this.SetRowSelection = function(row, isSelected) {
		//this.error = null;
		this.LogEntry('SetRowSelection'); 	
		this.InitControl();	
		try {
			if(isSelected) {
				this.SelectRow(row);
			}
			else {
				var appointment = this.GetAppointmentAt(row);
				if(this.control.appointmentSelection.IsAppointmentSelected(appointment.appointmentId)) {
					this.control.appointmentSelection.RemoveAppointmentFromSelection(appointment.appointmentId);							
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetRowSelection');
	}

	this.CheckAppointmentType = function(type) {
		//this.error = null;
		this.LogEntry('CheckAppointmentType'); 	
		this.InitControl();	
		try {
			var appointmentId = this.control.appointmentSelection.selectedAppointmentIds[0];
			var appointment = this.control.GetAppointmentById(appointmentId);
			if(appointment.appointmentType != type) {
				this.LogOperationError('AppointmentType is not ' + type + '. The actual type is ' + appointment.appointmentType);
			}
		}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CheckAppointmentType');
	}

	this.SetSelection = function(paramValues) {
		//this.error = null;
		this.LogEntry('SetSelection'); 	
		this.InitControl();	
		try {
			var from = paramValues.getItem(0);
			var to = paramValues.getItem(1);
			var fromTime = this.formatter.Parse(from);
			var toTime = this.formatter.Parse(to);
			if(fromTime && toTime) {
			var interval = new ASPxClientTimeInterval(fromTime, _aspxDateDiffInMilliseconds(toTime, fromTime));
			this.control.SetSelectionInterval(interval);
			}
			else {
			if(!fromTime) {
			this.LogOperationError('Incorrect starting date/time is specified: ' + from);
			}
			if(!toTime) {
			this.LogOperationError('Incorrect ending date/time is specified: ' + to);
			}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetSelection');
	}

	this.PopupMenuAction = function(menuItemName) {
		//this.error = null;
		this.LogEntry('PopupMenuAction'); 	
		this.InitControl();	
		try {
			var menuItemElement = this.GetMenuItemElement(menuItemName);
			if(menuItemElement && !this.operationError) {
				menuItemElement.click();
				return;
			}
			               if(!this.operationError) {
				this.LogOperationError('The ' + menuItemName + ' item is not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('PopupMenuAction');
	}

	this.HandleDeleteDialog = function(actionName) {
		//this.error = null;
		this.LogEntry('HandleDeleteDialog'); 	
		this.InitControl();	
		try {
			var buttons = _aspxGetChildrenByPartialClassName(deleteRecurrencePopupControl.windowElements[-1], 'dxbButton');
			var clientASPxButtons = new Array();
			for(var i = 0; i < buttons.length; i++) {
				clientASPxButtons[i] = eval(buttons[i].id.replace('_B', ''));
			}
			               if(deleteRecurrencePopupControl.IsVisible()) {
			    if(actionName == 'Cancel') {
				    var cancelButton = (clientASPxButtons[0].GetText() == 'Cancel') ? clientASPxButtons[0] : clientASPxButtons[1];
				    cancelButton.DoClick();
			    }
			    else if(actionName == 'DeleteOccurrence' || actionName == 'DeleteSeries') {
				    var okButton = (clientASPxButtons[0].GetText() == 'OK') ? clientASPxButtons[0] : clientASPxButtons[1];
				    var radioButtonList = _aspxGetChildrenByPartialClassName(deleteRecurrencePopupControl.windowElements[-1], 'dxeRadioButtonList')
				    var clientRadioButtonList = aspxGetControlCollection().Get(radioButtonList[0].id);
				    if(actionName == 'DeleteOccurrence') {
					    clientRadioButtonList.SetSelectedIndex(0);
				    }
				    else if(actionName == 'DeleteSeries') {
					    clientRadioButtonList.SetSelectedIndex(1);
				    }
				    okButton.DoClick();
			    }
			    else {
				    this.LogOperationError('Unrecognized Action name: ' + actionName);
			    }
			               }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('HandleDeleteDialog');
	}

	this.SetCurrentDate = function(dateString) {
		//this.error = null;
		this.LogEntry('SetCurrentDate'); 	
		this.InitControl();	
		try {
			var dateNavigator = eval(this.id + '_DateNavigator');
			var calendar = aspxGetControlCollection().Get(dateNavigator.calendarId);
			calendar.selection.Clear();
			try {
				calendar.selection.Add(this.formatter.Parse(dateString));
			}
			catch(e) {
				this.LogOperationError('Unable to parse \''+ dateString +'\' date with \'' + this.dateTimeFormatString +'\' mask.');
			}
			dateNavigator.OnSelectionChanged();
			this.control.RaiseSelectionChanged();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetCurrentDate');
	}

	this.CheckCurrentDate = function(dateString) {
		//this.error = null;
		this.LogEntry('CheckCurrentDate'); 	
		this.InitControl();	
		try {
			var selectedInterval = this.control.GetSelectedInterval();
			var startDate = ASPxSchedulerDateTimeHelper.TruncToDate(selectedInterval.GetStart());				
			var date = ASPxSchedulerDateTimeHelper.TruncToDate(this.formatter.Parse(dateString));
			return (startDate - date) == 0;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CheckCurrentDate');
	}

	this.GetTableRowCount = function() {
		//this.error = null;
		this.LogEntry('GetTableRowCount'); 	
		this.InitControl();	
		try {
			return this.control.horizontalViewInfo.appointmentViewInfos.length + this.control.verticalViewInfo.appointmentViewInfos.length;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetTableRowCount');
	}

	this.ClearSelection = function() {
		//this.error = null;
		this.LogEntry('ClearSelection'); 	
		this.InitControl();	
		try {
			this.control.appointmentSelection.ClearSelection();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ClearSelection');
	}

	this.ExecuteAction = function(actionName, row, column) {
		//this.error = null;
		this.LogEntry('ExecuteAction'); 	
		this.InitControl();	
		try {
			this.SelectRow(row);
			if(actionName != '') {
				this.PopupMenuAction(actionName);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExecuteAction');
	}

	this.SelectRow = function(row) {
		//this.error = null;
		this.LogEntry('SelectRow'); 	
		this.InitControl();	
		try {
			var appointment = this.GetAppointmentAt(row);
			if(!this.control.appointmentSelection.IsAppointmentSelected(appointment.appointmentId)) {
				this.control.appointmentSelection.AddAppointmentToSelection(appointment.appointmentId);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SelectRow');
	}

	this.UnselectRow = function(row) {
		//this.error = null;
		this.LogEntry('UnselectRow'); 	
		this.InitControl();	
		try {
			var appointment = this.GetAppointmentAt(row);
			if(this.control.appointmentSelection.IsAppointmentSelected(appointment.appointmentId)) {
				this.control.appointmentSelection.RemoveAppointmentFromSelection(appointment.appointmentId);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('UnselectRow');
	}

	this.IsRowSelected = function(row) {
		//this.error = null;
		this.LogEntry('IsRowSelected'); 	
		this.InitControl();	
		try {
			var appointment = this.GetAppointmentAt(row);
			return this.control.appointmentSelection.IsAppointmentSelected(appointment.appointmentId);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsRowSelected');
	}

	this.GetCellValue = function(row, column) {
		//this.error = null;
		this.LogEntry('GetCellValue'); 	
		this.InitControl();	
		try {
			var appointment = this.GetAppointmentAt(row);				
			if(column == this.startOnColumnIndex) {
				result = this.formatter.Format(appointment.getStartTime());
			}
			else if(column == this.endOnColumnIndex) {
				result = this.formatter.Format(appointment.getEndTime());
			}
			else if(column == this.subjectColumnIndex) {
				result = window.document.getElementById(appointment.contentDiv.firstChild.id.replace('appointmentDiv', 'lblTitle' )).innerText;
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetCellValue');
	}

	this.GetColumnIndex = function(columnCaption) {
		//this.error = null;
		this.LogEntry('GetColumnIndex'); 	
		this.InitControl();	
		try {
			var result = -1;
			if(columnCaption == 'StartOn') {
				result = this.startOnColumnIndex;
			}
			if(columnCaption == 'EndOn') {
				result = this.endOnColumnIndex;
			}
			if(columnCaption == 'Subject') {
				result = this.subjectColumnIndex;
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnIndex');
	}

	this.GetColumnsCaptions = function() {
		//this.error = null;
		this.LogEntry('GetColumnsCaptions'); 	
		this.InitControl();	
		try {
			return 'StartOn;EndOn;Subject'				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnsCaptions');
	}

	this.GetCellControl = function(row, column) {
		//this.error = null;
		this.LogEntry('GetCellControl'); 	
		this.InitControl();	
		try {
			var info = new Object();
			info.SchedulerControl = this;
			info.Row = row;
			info.Column = column;
			var result = new SchedulerInplaceEditor(id, column);
			result.Info = info;
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetCellControl');
	}

	this.SetCellValue = function(row, column, value) {
		//this.error = null;
		this.LogEntry('SetCellValue'); 	
		this.InitControl();	
		try {
			var appointment = this.GetAppointmentAt(row);
			if(column == this.startOnColumnIndex) {
				//this.SelectRow(row);
				var startTime = this.formatter.Parse(value);	
				this.control.GetAppointment(appointment.appointmentId).interval.SetStart(startTime);
				var params = 'APTSCHANGE|' + appointment.appointmentId;
				params += '?START='+  _aspxDateTimeToMilliseconds(startTime);
				params += '?DURATION=' +  _aspxDateDiffInMilliseconds(appointment.getEndTime(), startTime);
				this.control.RaiseCallback(params);
				return;
			}
			else if(column == this.endOnColumnIndex) {
				//this.SelectRow(row);
				var endTime = this.formatter.Parse(value);
				this.control.GetAppointment(appointment.appointmentId).interval.SetEnd(endTime);
				var params = 'APTSCHANGE|' + appointment.appointmentId;
				params += '?START='+  _aspxDateTimeToMilliseconds(appointment.getStartTime());
				params += '?DURATION=' +  _aspxDateDiffInMilliseconds(endTime, appointment.getStartTime());
				this.control.RaiseCallback(params);     
				return;
			}
			else if(column == this.subjectColumnIndex) {
				this.LogOperationError('You can not change the subject of an event');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetCellValue');
	}
}
/*		ASPxPopupCriteriaPropertyEditor		*/
function ASPxPopupCriteriaPropertyEditor(id, caption) {
	this.className = 'ASPxPopupCriteriaPropertyEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			debugger;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var button = eval(this.id + '_B0')
			            button.onclick();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		SchedulerInplaceEditor		*/
function SchedulerInplaceEditor(id, caption) {
	this.className = 'SchedulerInplaceEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.InitControl = function() {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
		}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.Info.SchedulerControl.SetCellValue(this.Info.Row, this.Info.Column, value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}
}
/*		ASPxCriteriaPropertyEditor		*/
function ASPxCriteriaPropertyEditor(id, caption, criteriaEditor) {
	this.className = 'ASPxCriteriaPropertyEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.criteriaEditor = criteriaEditor;
	this.PatchFilterString = function(filterString) {
		//this.error = null;
		this.LogEntry('PatchFilterString'); 	
		this.InitControl();	
		try {
			return criteriaEditor.cpPatchedFilterString;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('PatchFilterString');
	}

	this.AddCondition = function(groupIndex) {
		//this.error = null;
		this.LogEntry('AddCondition'); 	
		this.InitControl();	
		try {
			groupIndex = eval(groupIndex);
			this.ExecuteGroupOperation("|AddCondition", groupIndex);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('AddCondition');
	}

	this.RemoveCriteria = function(index) {
		//this.error = null;
		this.LogEntry('RemoveCriteria'); 	
		this.InitControl();	
		try {
			criteriaEditor.RemoveNode(eval(index) + 1);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('RemoveCriteria');
	}

	this.InitControl = function() {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			criteriaEditor = eval('window.' +this.id);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.CheckEditorType = function(editorTypeName) {
		//this.error = null;
		this.LogEntry('CheckEditorType'); 	
		this.InitControl();	
		try {
			debugger;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CheckEditorType');
	}

	this.CheckValue = function(nodeIndex, expectedValue) {
		//this.error = null;
		this.LogEntry('CheckValue'); 	
		this.InitControl();	
		try {
			var postfix = nodeIndex + '000';
			var editorId = 'DXValue'+ postfix;
			var valueEditorId = criteriaEditor.name + '_DXEdit' + postfix + '_ValueEditor';
			var valueEditor = eval('window.' + valueEditorId);
			var actualValue = criteriaEditor.GetChildElementById(editorId).innerText;
			if(valueEditor) {
			    if(valueEditor.GetVisible && valueEditor.GetVisible() && valueEditor.GetText) {
			        actualValue = valueEditor.GetText();
			    }
			}
			if(actualValue =='' || actualValue == '<enter a value>') {
			    actualValue = '?';
			}
			if(actualValue != expectedValue) {
			    this.LogOperationError('Node value differs from the expected value. Expected: \'' + expectedValue + '\', but was: \'' + actualValue + '\'');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('CheckValue');
	}

	this.ShowEditor = function(index) {
		//this.error = null;
		this.LogEntry('ShowEditor'); 	
		this.InitControl();	
		try {
			criteriaEditor.ShowEditor(index);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ShowEditor');
	}

	this.ChangeGroupType = function(groupIndex, newGroupCaption) {
		//this.error = null;
		this.LogEntry('ChangeGroupType'); 	
		this.InitControl();	
		try {
			this.ExecuteGroupOperation(newGroupCaption, eval(groupIndex));				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ChangeGroupType');
	}

	this.AddGroup = function(sourceGroupIndex) {
		//this.error = null;
		this.LogEntry('AddGroup'); 	
		this.InitControl();	
		try {
			this.ExecuteGroupOperation("|AddGroup", sourceGroupIndex);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('AddGroup');
	}

	this.ExecuteGroupOperation = function(operationName, groupIndex) {
		//this.error = null;
		this.LogEntry('ExecuteGroupOperation'); 	
		this.InitControl();	
		try {
			var filterControlGroupPopup = eval('window.' +this.id +'_GroupPopup');
			criteriaEditor.ShowGroupPopup(0,groupIndex);
			var item = filterControlGroupPopup.GetItemByName(operationName);
			if(item) {
				filterControlGroupPopup.DoItemClick(item.index.toString() ,'','');
			}
			else {
				this.LogOperationError('Unable to find ' + operationName + ' item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExecuteGroupOperation');
	}

	this.ChangeOperation = function(rowIndex, newOperationCaption) {
		//this.error = null;
		this.LogEntry('ChangeOperation'); 	
		this.InitControl();	
		try {
			var preparedCaption = newOperationCaption.replace(/ /g,'').toLowerCase();
			var filterControlOperationPopup = eval('window.' +this.id +'_OperationPopup');
			var rootItem = filterControlOperationPopup.rootItem;
			criteriaEditor.ShowOperationPopup(0, eval(rowIndex).toString());
			var item;
			for(var i = 0; i < rootItem.items.length; i++) {
			var shortItemName = rootItem.items[i].name.split('|')[1];
			if(shortItemName.toLowerCase() == preparedCaption) {
			item = rootItem.items[i];
			break;
			}
			}
			if(item) {
			filterControlOperationPopup.DoItemClick(item.index.toString(),'','');
			}
			else {
			this.LogOperationError('Unable to find ' + newOperationCaption + ' item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ChangeOperation');
	}

	this.ChangeValue = function(rowIndex, newValue) {
		//this.error = null;
		this.LogEntry('ChangeValue'); 	
		this.InitControl();	
		try {
			var postfix = rowIndex + '000';
			var editorId = this.id + '_DXEdit'+ postfix;
			var editor = eval(editorId);
			                  var valueEditor = criteriaEditor.GetChildElementById('DXValue'+ postfix);
			                  valueEditor.click();
			if(editor.isASPxClientTextEdit && editor.isDropDownListStyle) {
				if(newValue.indexOf('@')== 0 || newValue == '<enter a value>') {
			                          var testControl = new ASPxComboBox(editor.name,'',false);
			                          testControl.Act(newValue);
			                          if(testControl.GetText() != newValue) {
			                              this.LogOperationError('Unable to find ' + newValue + ' item.');
			                          }
			                          editor.RaiseLostFocus();
				}
				else {
				    var valueEditorId = editorId +"_ValueEditor";
				    var valueEditor = eval('window.' + valueEditorId);
			                          if(valueEditor && !editor.cpHasNoValueWithParametersEdit) {
			                              valueEditor.Filter = criteriaEditor;
			                              editor.RaiseCloseUp();
				        this.SetValueWithEditor(valueEditorId, newValue);
			                          }
			                          else {
			                              this.SetValueWithEditor(editorId, newValue);
			                          }
				}
			}
			else {
				this.SetValueWithEditor(editorId, newValue);
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ChangeValue');
	}

	this.SetValueWithEditor = function(editorId, newValue) {
		//this.error = null;
		this.LogEntry('SetValueWithEditor'); 	
		this.InitControl();	
		try {
			var editor = eval(editorId);
			var fieldCaption = editor.GetMainElement().parentElement.parentElement.childNodes[0].innerText;
			                     fieldCaption = fieldCaption.replace(/ /g,'');
			                     var isFound = false;
			for(var i = 0; i < TestControls.testControlsDescriptions.length; i++) {
				if(TestControls.testControlsDescriptions[i].caption == fieldCaption) {
					var properTestControl = eval("new " + TestControls.testControlsDescriptions[i].className + "('" + editorId +"','" + fieldCaption + "',false)");
					properTestControl.SetText(newValue);
			                             isFound = true;
					break;
				}
			}
			                     if(!isFound) {
			                         this.LogOperationError('The \'' + fieldCaption + '\' property was not found.');
			                     }
			editor.RaiseLostFocus();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetValueWithEditor');
	}

	this.ChangeFieldName = function(rowIndex, newValue) {
		//this.error = null;
		this.LogEntry('ChangeFieldName'); 	
		this.InitControl();	
		try {
			var fieldName = newValue.replace(' ','');
			criteriaEditor.ShowFieldNamePopup('',eval(rowIndex))
			var filterControlFieldNamePopup = eval('window.' +this.id +'_FieldNamePopup');
			var item = filterControlFieldNamePopup.GetItemByName(fieldName);
			if(item) {
			var itemIndex = item.index;
			filterControlFieldNamePopup.DoItemClick(itemIndex.toString(), '', '');
			}
			else {
			    this.LogOperationError('The \'' + newValue + '\' property was not found.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ChangeFieldName');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.LogOperationError('The "' + this.caption + '" editor is readonly.');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.PatchFilterString(criteriaEditor.GetAppliedFilterExpression());				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var param = value.split('|');
			for (var i = 1; i < param.length; i++) {
			 param[i]  = '\'' + param[i].replace(/'/g, '\\\'') + '\'';
			}
			eval('this.' + param[0] + '(' + param.slice(1) + ');');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		DefaultControl		*/
function DefaultControl(id, caption) {
	this.className = 'DefaultControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}
}
/*		TextBox		*/
function TextBox(id, caption) {
	this.className = 'TextBox';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.isContentEditable)) {
				if(!this.control.isContentEditable) {
					this.LogOperationError('The ' + this.caption + ' editor is read-only.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}
}
/*		Label		*/
function Label(id, caption) {
	this.className = 'Label';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.FindLabel = function(control) {
		//this.error = null;
		this.LogEntry('FindLabel'); 	
		this.InitControl();	
		try {
			var result;
			if(control) {
			    for(var i=0;i<control.childNodes.length;i++) {
			        if(result) { return result; }
			        if(control.tagName == 'LABEL') {
			            return control;
			        }
			        else {
			            result = this.FindLabel(control.childNodes[i]);
			        }
			    }
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('FindLabel');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			this.LogOperationError('The text displayed by a Label control cannot be changed');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			//B153845 Bug Fix, kest
			if(this.control.tagName == 'TABLE') {
			    var label = this.FindLabel(this.control);
			    if(label) {
			        return this.FindLabel(this.control).innerText;
			    }
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			//B38639
			if(!this.IsEnabled()) {
			       this.LogOperationError('The Label control is disabled');
			} else {
				//Commented because of (AB8572) bug fix 
				//Uncommented because of (B36753) bug fix 
					if(this.GetText() == '' || this.control.tagName.toUpperCase()!='A') {
					this.LogOperationError('No Actions are available');
				}
				else {
					this.control.click();				
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		CheckBox		*/
function CheckBox(id, caption) {
	this.className = 'CheckBox';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.InitControl = function() {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			var f = this.inherit.prototype.baseInitControl;
			var oldId = this.id;
			this.id = this.id + 'Input';
			               try {
			    f.call(this);
			               } 
			               catch(ex) {
				this.error = null;
				this.operationError = false;
				this.id = oldId;
				f.call(this);
			               }				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(value != 'True' && value != 'False') {
				this.LogOperationError('Boolean properties can be set to either True or False. The ' + value + ' is not a valid value.');
			}				
			this.control.checked = (value == 'True') ? true : false;
			this.control.fireEvent('onchange');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return (this.control.checked) ? 'True' : 'False';				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}
}
/*		DropDownList		*/
function DropDownList(id, caption, autoPostBack) {
	this.className = 'DropDownList';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.autoPostBack = autoPostBack;
	this.IsAutoPostBackMethod = function(methodName) {
		//this.error = null;
		this.LogEntry('IsAutoPostBackMethod'); 	
		this.InitControl();	
		try {
			if(methodName == 'SetText' && autoPostBack) {
				return true;
			}
			return false;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsAutoPostBackMethod');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var isFound = false;				
			for(var i = 0; i < this.control.options.length; i++) {
				var option = this.control.options[i];
				if (option.text == value) {
					isFound = true;
					this.control.selectedIndex = i;
					this.control.fireEvent('onchange');
					break;
				}
			}
			if (!isFound) {					
				this.LogOperationError('Cannot change the ' + this.caption + ' control\'s value. The list of available items does not contain the specified value');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.selectedIndex != -1) {
				return this.control.options[this.control.selectedIndex].innerText;
			}
			else {
				return '';
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			this.SetText(value);				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		LookupEdit		*/
function LookupEdit(id, caption) {
	this.className = 'LookupEdit';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.rows[0].cells[0].childNodes[0].childNodes[0].value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.rows[0].cells[0].childNodes[0].childNodes[0].readOnly;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var button = this.control.rows[0].cells[1].childNodes[0];
			if(!button.disabled) {
				this.control.rows[0].cells[1].childNodes[0].click();
			} else {
				this.LogOperationError('Cannot execute the Edit Action in the ' + this.caption + ' control');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		LookupDropDownEdit		*/
function LookupDropDownEdit(id, caption) {
	this.className = 'LookupDropDownEdit';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var isFound = false;				
			var control = this.control.rows[0].cells[0].childNodes[0];
			for(var i = 0; i < control.options.length; i++) {
				var option = control.options[i];
				if (option.text == value) {
					isFound = true;
					control.selectedIndex = i;
					control.fireEvent('onchange');
					break;
				}
			}
			if (!isFound) {						
				this.LogOperationError('Cannot change the ' + this.caption + ' control\'s value. The list of available values does not contain the specified value');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var control = this.control.rows[0].cells[0].childNodes[0];
			return control.options[control.selectedIndex].text;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(this.control.rows[0] && this.control.rows[0].cells[1] && this.control.rows[0].cells[1].childNodes[0] && this.control.rows[0].cells[1].childNodes[0].click) {
				this.control.rows[0].cells[1].childNodes[0].click();
			} else {
				this.LogOperationError('The "' + value + '" Action is inactive');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		GridEditor		*/
function GridEditor(id, caption) {
	this.className = 'GridEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.InitControl = function() {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			if(this.control.tagName.toUpperCase() != 'TABLE') {
				this.control = null;
			} else {
				this.control = this.control;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetActionColumnCount = function() {
		//this.error = null;
		this.LogEntry('GetActionColumnCount'); 	
		this.InitControl();	
		try {
			var table = this.control;
			if(table) {
				for(var i = 0; i < table.rows[0].cells.length; i++) {
					if(table.rows[0].cells[i].innerText != ' ') {
						return i;
					}
				}
			}
			return 0;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetActionColumnCount');
	}

	this.SortByColumn = function(columnCaption) {
		//this.error = null;
		this.LogEntry('SortByColumn'); 	
		this.InitControl();	
		try {
			var table = this.control;
			var isFound = false;
			if(table) {
				for(var i = this.GetActionColumnCount(); i < table.rows[0].cells.length; i++) {
					if(table.rows[0].cells[i].innerText == columnCaption) {
						isFound = true;
						table.rows[0].cells[i].childNodes[0].click();
						break;
					}
				}
			}
			if(!isFound) {
				this.LogOperationError( 'The grid does not contain the ' + columnCaption + ' column.');
			}										                    				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SortByColumn');
	}

	this.GetTableRowCount = function() {
		//this.error = null;
		this.LogEntry('GetTableRowCount'); 	
		this.InitControl();	
		try {
			var table = this.control;
			if(table) {
			  return table.rows.length - 1;
			}
			            return 0;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetTableRowCount');
	}

	this.ExecuteAction = function(actionName, row, column) {
		//this.error = null;
		this.LogEntry('ExecuteAction'); 	
		this.InitControl();	
		try {
			var table = this.control;
			if(!table) this.LogOperationError('Cannot perform an Action. The grid doesn\'t contain any records');
			if(row + 1 >= table.rows.length) {
				var maxRows = table.rows.length - 1;
				this.LogOperationError( 'The grid contains: ' + maxRows + ' rows');
				return '';
			}
			this.callStack += '< action: ' + actionName + '\r\n';
			if(actionName != '') {					
				var isFound = false;
				for(var i = 0; i < this.GetActionColumnCount(); i++) {						
					if(table.rows[row + 1].cells[i].innerText != '' || table.rows[row + 1].cells[i].innerText == ' ') {							
						isFound = table.rows[row + 1].cells[i].innerText == actionName;
					}
					else if(table.rows[row + 1].cells[i].childNodes.length > 0) {
						isFound = table.rows[row + 1].cells[i].childNodes[0].childNodes[0].alt == actionName;
					}
					if(isFound) {
						table.rows[row + 1].cells[i].childNodes[0].click();
						break;
					}
				}					
				if(!isFound) {
					this.LogOperationError( 'The ' + actionName + ' Action is not found in the ' + this.Caption + ' table\'s ' + row + ' row.');
				}
			}
			else {
				this.callStack += '< defaul ACTION \r\n';
			                var maxColumn = table.rows[row + 1].cells.length - this.GetActionColumnCount()
			                if(column >= maxColumn) {
			                   this.LogOperationError( 'The grid contains ' + maxColumn + ' columns');
			                }
				this.callStack += '< row, column, text: ' + row + ', ' + column + ', ' + table.rows[row + 1].cells[column + this.GetActionColumnCount()].innerText + '\r\n';
				table.rows[row + 1].cells[column + this.GetActionColumnCount()].click();
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('ExecuteAction');
	}

	this.GetCellValue = function(row, column) {
		//this.error = null;
		this.LogEntry('GetCellValue'); 	
		this.InitControl();	
		try {
			var table = this.control;
			result = '';
			if(table) {
				if(row + 1 >= table.rows.length) {
					var maxRows = table.rows.length - 1;
					this.LogOperationError( 'The grid contains ' + maxRows + ' rows');
					return result;
				}			
				var cell = table.rows[row + 1].cells[this.GetActionColumnCount() + column];
				if(cell.innerText == '' && cell.childNodes.length == 1) {
					var img = cell.childNodes[0];
					if(!IsNull(img.title)) {
						result = img.title;
					}
				}			
				else {
					result = table.rows[row + 1].cells[this.GetActionColumnCount() + column].innerText;
					if(result == ' ') result = '';
				}		
			}	
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetCellValue');
	}

	this.GetColumnIndex = function(columnCaption) {
		//this.error = null;
		this.LogEntry('GetColumnIndex'); 	
		this.InitControl();	
		try {
			var table = this.control;
			if(table) {
				for(var i = this.GetActionColumnCount(); i < table.rows[0].cells.length; i++) {
					if(table.rows[0].cells[i].innerText == columnCaption) {
						return i - this.GetActionColumnCount();
					}
				}
			}
			else {
				return -1;
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnIndex');
	}

	this.GetColumnsCaptions = function() {
		//this.error = null;
		this.LogEntry('GetColumnsCaptions'); 	
		this.InitControl();	
		try {
			var result = '';
			var table = this.control;
			if(table) {
				for(var i = this.GetActionColumnCount(); i < table.rows[0].cells.length; i++) {
					if(result == '') {
						result = table.rows[0].cells[i].innerText;
					} 
					else {
						result += ';' + table.rows[0].cells[i].innerText;
					}
				}
			}
			return result;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetColumnsCaptions');
	}
}
/*		WebWorkflowEditor		*/
function WebWorkflowEditor(id, caption) {
	this.className = 'WebWorkflowEditor';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			var controlTable = this.control.childNodes[0];
			var textEdit = controlTable.rows[0].cells[0].childNodes[0];
			textEdit.value = value;
			textEdit.fireEvent('onchange');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			var controlTable = this.control.childNodes[0];
			var textEdit = controlTable.rows[0].cells[0].childNodes[0];
			return textEdit.value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var controlTable = this.control.childNodes[0];
			var buttons = controlTable.rows[0].cells[1].childNodes;
			var isFound = false;
			for(var i = 0; i < buttons.length; i++) {
				if(buttons[i].innerText == value) {
					isFound = true;
					buttons[i].fireEvent('onclick');
					break;
				}
			}
			if(!isFound) {
				this.LogOperationError( 'Cannot find the ' + value + ' Action.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		Menu		*/
function Menu(id, caption) {
	this.className = 'Menu';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var menuTable = this.control;
			if(IsNull(value)){
				var rootMenuItemTable = menuTable.rows[0].cells[0].childNodes[0];
				var rootMenuItem = rootMenuItemTable.rows[0].cells[0].childNodes[0];
				rootMenuItem.click();
			}
			else {				
				var menuItemsDiv = document.getElementById(this.id + 'n0Items');
				var menuItemsTable = menuItemsDiv.childNodes[0];
				for(var i = 0; i < menuItemsTable.rows.length; i++){
					var menuItem = menuItemsTable.rows[i].cells[0].childNodes[0];
					if(menuItem.innerText == value) {
						menuItem.childNodes[0].rows[0].cells[0].childNodes[0].click();
						break;
					}
				}
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		GroupedListItem		*/
function GroupedListItem(id, caption) {
	this.className = 'GroupedListItem';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var tableControl = this.control;
			var isFound = false;
			for(var i = 0; i < tableControl.rows.length; i++) {
				if(tableControl.rows[i].cells[0].innerText == value) {
					isFound = true;
					if(!tableControl.rows[i].cells[0].childNodes[0].childNodes[0].disabled) {
						tableControl.rows[i].cells[0].childNodes[0].childNodes[0].click();
					}
					else {
						this.LogOperationError( 'The ' + value + ' item is disabled.');
					}
					break;
				}
			}
			if(!isFound) {
				this.LogOperationError( 'Cannot find the ' + value + ' item.');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		TextItem		*/
function TextItem(id, caption) {
	this.className = 'TextItem';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.childNodes[0].value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			this.control.childNodes[0].value = value;
			this.control.childNodes[1].click();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		Button		*/
function Button(id, caption) {
	this.className = 'Button';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.GetHint = function() {
		//this.error = null;
		this.LogEntry('GetHint'); 	
		this.InitControl();	
		try {
			return this.control.title;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetHint');
	}

	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {

			var button = this.control;
			if(this.control.tagName == 'SPAN') {
			button = this.control.childNodes[0];
			}
			if(button.disabled)
			      this.LogOperationError('No Actions are available');
			else
			button.click();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		ParametrizedAction		*/
function ParametrizedAction(id, caption) {
	this.className = 'ParametrizedAction';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			return this.control.GetText();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			hasMainElementMethod = false;
			isMainElementEnabled = false;
			if(this.control.GetMainElement) {
				hasMainElementMethod = true;
				isMainElementEnabled = !this.control.GetMainElement().isDisabled;
			}
			hasGetEnabledMethod = false;
			isEnabled = false;
			if(this.control.GetEnabled) {
				hasGetEnabledMethod = true;
				isEnabled = this.control.GetEnabled();
			}
			//B150245
			if((hasGetEnabledMethod && hasMainElementMethod) && (isEnabled && !isMainElementEnabled)){
				return isMainElementEnabled;
			}
			if(hasGetEnabledMethod) {
				return isEnabled;
			}
			if(this.control.enabled != true && this.control.enabled != false) {
				if(this.control.GetInputElement) {
					return !this.control.GetInputElement().isDisabled;
				} else {
					return false;
				}
			}
			return this.control.enabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(!this.control.GetEnabled()) {
				this.LogOperationError('Cannot enter a parameter value into the ' + this.caption + ' Action. The element is disabled');
				return;
			}
			this.control.SetText(value);
			this.control.DoClick();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		JSBrowserNavigationTestControl		*/
function JSBrowserNavigationTestControl(id, caption) {
	this.className = 'JSBrowserNavigationTestControl';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return !this.control.disabled;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			if(value == 'back')
				history.back();
			else if(value == 'forward')
				history.forward();
			else
				this.LogOperationError('Only the "back" and "forward" parameters are supported');				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}
}
/*		ASPxButton		*/
function ASPxButton(id, caption) {
	this.className = 'ASPxButton';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var aspxButton = eval(this.id);
			if(!aspxButton) {
				this.LogOperationError('Client-side API is disabled for the ' + this.caption);
				return;
			}
			aspxButton.DoClick();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}

	this.GetHint = function() {
		//this.error = null;
		this.LogEntry('GetHint'); 	
		this.InitControl();	
		try {
			return this.control.GetMainElement().title;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetHint');
	}
}
/*		ASPxTab		*/
function ASPxTab(id, caption) {
	this.className = 'ASPxTab';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var aspxControl;
			eval('aspxControl = ' + this.id);
			var control = this.control;
			var executableControl = null;
			var groupCount = aspxControl.GetTabCount() ? aspxControl.GetTabCount() : aspxControl.tabCount;
			var itemCount = groupCount;
			var title = '';
			if(this.caption.lastIndexOf('.') != -1) {
				title = this.caption.substr(this.caption.lastIndexOf('.') + 1);
			}
			else {
				title = this.caption;
			}
			var tabsToSearch = aspxControl.tabs;
			for(var i = 0; i < tabsToSearch.length; i++){
				if(tabsToSearch[i].GetVisible() && _aspxDecodeHtml(tabsToSearch[i].GetText()) == title) {
					executableControl = aspxControl.GetTab(i);
					break;
				}
			}
			if(executableControl) {
				aspxControl.SetActiveTab(executableControl);
			}
			else {
				this.LogOperationError('The "' + this.caption + '" Action does not contain the "' + value + '" item');
			}				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}
}
/*		ASPxPopupWindowButton		*/
function ASPxPopupWindowButton(id, caption) {
	this.className = 'ASPxPopupWindowButton';
	this.inherit = TestControlBase;
	this.inherit(id, caption);
	this.SetText = function(value) {
		//this.error = null;
		this.LogEntry('SetText'); 	
		this.InitControl();	
		try {
			if(!IsNull(this.control.readOnly)) {
				if(this.control.readOnly) {
					this.LogOperationError('The "' + this.caption + '" editor is readonly.');
					return;
				}
			}
			this.control.value = value;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('SetText');
	}

	this.GetText = function() {
		//this.error = null;
		this.LogEntry('GetText'); 	
		this.InitControl();	
		try {
			if(this.control.value) {
				return this.control.value;
			}
			return this.control.innerText;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetText');
	}

	this.IsEnabled = function() {
		//this.error = null;
		this.LogEntry('IsEnabled'); 	
		this.InitControl();	
		try {
			return this.control.GetEnabled();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('IsEnabled');
	}

	this.Act = function(value) {
		//this.error = null;
		this.LogEntry('Act'); 	
		this.InitControl();	
		try {
			var aspxButton = eval(this.id);
			if(!aspxButton) {
				this.LogOperationError('Client-side API is disabled for the ' + this.caption);
				return;
			}
			aspxButton.DoClick();				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('Act');
	}

	this.InitControl = function(value) {
		//this.error = null;
		this.LogEntry('InitControl'); 	
		try {
			eval('this.control = window.' + this.id.replace(/\$/g,'_'));
			if(this.control) {
				return;
			}
			var f = this.inherit.prototype.baseInitControl;
			f.call(this);
			if(this.error) {
				return;
			}
			//eval('this.control = ' + this.id.replace('$', '_'));						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('InitControl');
	}

	this.GetClientControlById = function(id) {
		//this.error = null;
		this.LogEntry('GetClientControlById'); 	
		this.InitControl();	
		try {
			var result;
			eval('result = window.' + id);
			return result;						}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetClientControlById');
	}

	this.GetHint = function() {
		//this.error = null;
		this.LogEntry('GetHint'); 	
		this.InitControl();	
		try {
			return this.control.GetMainElement().title;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetHint');
	}

	this.GetIsPopupWindow = function() {
		//this.error = null;
		this.LogEntry('GetIsPopupWindow'); 	
		this.InitControl();	
		try {
			return true;				}
		catch(e) {					
			if(e.operationError) {
				this.LogOperationError(e.message);
			} 
			else {
	                     if(!this.operationError) {
				    this.LogError(e.message);
	                         throw e;
	                     }
			}
		}
		this.LogExit('GetIsPopupWindow');
	}
}