// Name, Path, FullPath
var simpleFile = function (path) {
  var parts = path.split('/');
  this.Name = parts.pop();
  this.IsFolder = false;
  if (this.Name.length === 0) {
    this.IsFolder = true;
    this.Name = parts.pop();
  } else {
    var extparts = this.Name.split('.');
    this.Extension = extparts.length > 1 ? extparts[extparts.length - 1] : '';
  }
  this.Path = '/';
  if (parts.length > 0) {
    this.Path = parts.join('/') + '/';
  }
  this.FullPath = this.Path + this.Name;
  //console.log(this.IsFolder + '    ' + this.Name + '    ' + this.Path + '   ' + this.FullPath);
};

var fileManager = function (config) {

  var _basePath = config.basePath;
  var _$listElement = config.listElement;
  var _template = config.template;
  var _baseDataObject = config.baseDataObject;

  var _this = this;

  if (!_basePath || !_$listElement || !_template || !_baseDataObject) {
    alert('ERROR in fileManger configuration');
    return;
  }

  // Events
  var FOLDER_CHANGING_EVENT = 'folderchanging';
  var _folderChanging = function () {
    var evt = $.Event(FOLDER_CHANGING_EVENT);
    $(window).trigger(evt);
  };
  var _onFolderChanging = function (func) {
    $(window).on(FOLDER_CHANGING_EVENT, func);
  };
  this.onFolderChanging = _onFolderChanging;

  var FOLDER_CHANGED_EVENT = 'folderchanged';
  var _folderChanged = function () {
    var evt = $.Event(FOLDER_CHANGED_EVENT);
    $(window).trigger(evt);
  };
  var _onFolderChanged = function (func) {
    $(window).on(FOLDER_CHANGED_EVENT, func);
  };
  this.onFolderChanged = _onFolderChanged;

  // Global Properties
  this.ActiveFilePath = '';
  this.RootFolder = null;
  this.FolderPaths = [];

  // Methods
  var _loadFolder = function () {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.ajax({
        url: _basePath + '/GetFolder',
        data: _baseDataObject,
        type: 'POST',
        dataType: 'json'
      }).done(function (folder) {
        _this.RootFolder = folder;
        _this.FolderPaths = [];
        if (_this.ActiveFilePath && _this.ActiveFilePath.length > 0) {
          _this.ActiveFilePath = _this.ActiveFilePath.replace('//', '/');
        }
        _processFolder(folder, '/', 0);

        var rendered = Mustache.render(_template, folder, { 'folder': _template });
        _$listElement.html(rendered);
        _folderChanged();
        resolve();
      }).fail(function (xhr, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.loadFolder = _loadFolder;

  var _processFolder = function (folder, path, level) {
    _this.FolderPaths.push(path);
    for (var i = 0; i < folder.Folders.length; i++) {
      var sub = folder.Folders[i];
      sub.Id = createGuid();
      sub.Path = path + sub.Name + '/';
      sub.Level = level;
      _processFolder(sub, sub.Path, level + 1);
    }
    for (var i = 0; i < folder.Files.length; i++) {
      var file = folder.Files[i];
      file.Folder = path;
      file.Path = path + file.Name;
      file.Level = level;
      if (!_this.ActiveFilePath || _this.ActiveFilePath.length === 0) {
        file.Selected = (i === 0 && path === '/') ? 'selected' : '';
      } else {
        file.Selected = file.Path === _this.ActiveFilePath ? 'selected' : '';
      }
    }
  };

  var _newFile = function (name, path, template) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/NewFile',
        data: $.extend({}, _baseDataObject, {
          name: name,
          path: path,
          template: template
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('File or folder with that name already exists'));
        } else {
          _this.ActiveFilePath = path + '/' + name;
          _loadFolder().then(resolve, reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.newFile = _newFile;

  var _newFolder = function (name, path) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/NewFolder',
        data: $.extend({}, _baseDataObject, {
          name: name,
          path: path
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('File or folder with that name already exists'));
        } else {
          _loadFolder().then(resolve, reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.newFolder = _newFolder;

  var _copyFile = function (file, newName, newPath) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileCopy',
        data: $.extend({}, _baseDataObject, {
          name: file.Name,
          path: file.Path,
          newName: newName,
          newPath: newPath
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('File or folder with that name already exists'));
        } else {
          _loadFolder().then(resolve,reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.copyFile = _copyFile;

  var _copyFolder = function (folder, newName, newPath) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileCopy',
        data: $.extend({}, _baseDataObject, {
          name: folder.Name,
          path: folder.Path,
          newName: newName,
          newPath: newPath
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('File or folder with that name already exists'));
        } else {
          _loadFolder().then(resolve, reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.copyFolder = _copyFolder;

  var _moveFile = function (file, newPath) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileMove',
        data: $.extend({}, _baseDataObject, {
          name: file.Name,
          path: file.Path,
          newName: file.Name,
          newPath: newPath
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('File or folder with that name already exists'));
        } else {
          // if moving selected file, point to moved file
          if (file.FullPath === _this.ActiveFilePath) {
            _this.ActiveFilePath = newPath + '/' + file.Name;
          }
          _loadFolder().then(resolve,reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.moveFile = _moveFile;

  var _moveFolder = function (folder, newPath) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileMove',
        data: $.extend({}, _baseDataObject, {
          name: folder.Name,
          path: folder.Path,
          newName: folder.Name,
          newPath: newPath
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('Folder or folder with that name already exists'));
        } else {
          _loadFolder().then(resolve, reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.moveFolder = _moveFolder;

  var _renameFile = function (file, newName) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileMove',
        data: $.extend({}, _baseDataObject, {
          name: file.Name,
          path: file.Path,
          newName: newName,
          newPath: file.Path
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('File or folder with that name already exists'));
        } else {
          // if renaming selected file, point to renamed file
          if (file.FullPath === _this.ActiveFilePath) {
            _this.ActiveFilePath = file.Path + '/' + newName;
          }
          _loadFolder().then(resolve,reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.renameFile = _renameFile;

  var _renameFolder = function (folder, newName) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileMove',
        data: $.extend({}, _baseDataObject, {
          name: folder.Name,
          path: folder.Path,
          newName: newName,
          newPath: folder.Path
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        if (data === 'conflict') {
          _folderChanged();
          reject(Error('Folder or folder with that name already exists'));
        } else {
          // if renaming selected file, point to renamed file
          if (_this.ActiveFilePath.startsWith(folder.FullPath)) {
            var newPath = folder.Path + '/' + newName;
            _this.ActiveFilePath = _this.ActiveFilePath.replace(folder.FullPath, newPath);
          }
          _loadFolder().then(resolve, reject);
        }
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.renameFolder = _renameFolder;

  var _deleteFile = function (file) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileDelete',
        data: $.extend({}, _baseDataObject, {
          name: file.Name,
          path: file.Path
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        // if deleting selected file, reset to "default" file
        if (file.FullPath === _this.ActiveFilePath) {
          _this.ActiveFilePath = '';
        }
        _loadFolder().then(resolve,reject);
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.deleteFile = _deleteFile;

  var _deleteFolder = function (folder) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/FileDelete',
        data: $.extend({}, _baseDataObject, {
          name: folder.Name,
          path: folder.Path
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        // if active file is in deleted folder, reset to default
        if (_this.ActiveFilePath.startsWith(folder.FullPath)) {
          _this.ActiveFilePath = '';
        }
        _loadFolder().then(resolve, reject);
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.deleteFolder = _deleteFolder;

  var _upload = function (htmlFiles, path) {
    return new Promise(function (resolve, reject) {
      if (!htmlFiles.length) {
        // just a single file
        htmlFiles = [htmlFiles];
      }
      var errorText = '';

      var realFiles = [];
      for (var i = 0; i < htmlFiles.length; i++) {
        var file = htmlFiles[i];
        if (file.type && file.size) {
          var size = Math.round(file.size / 1024 / 102.4) / 10;
          if (size > 5) {
            errorText += file.name + ' is larger than 5MB and cannot be uploaded.\n';
          } else {
            realFiles.push(file);
          }
        } else {
          if (file.name) {
            errorText += file.name + ' cannot be uploaded because it is either a folder or an unknown file type.\n';
          }
          //// no mimetype:  might be an unknown file or a directory, check
          //try {
          //  // attempt to access the first few bytes of the file, will throw an exception if a directory
          //  new FileReader().readAsBinaryString(file.slice(0, 5));
          //  // no exception, a file
          //  realFiles.push(file);
          //} catch (e) {
          //  // could not access contents, is a directory, skip
          //}
        }
      }

      var fileCount = realFiles.length;
      if (fileCount === 0) {
        reject(Error(errorText));
        return;
      }

      _folderChanging();

      for (var i = 0; i < realFiles.length; i++) {
        var htmlFile = realFiles[i];

        var formData = new FormData();
        for (var id in _baseDataObject) {
          formData.append(id, _baseDataObject[id]);
        }
        formData.append('name', htmlFile.name);
        formData.append('path', path);
        formData.append('file', htmlFile);

        var jqXHR = $.ajax({
          xhr: function () {
            var xhrobj = $.ajaxSettings.xhr();
            if (xhrobj.upload) {
              xhrobj.upload.addEventListener('progress', function (event) {
                var percent = 0;
                var position = event.loaded || event.position;
                var total = event.total;
                if (event.lengthComputable) {
                  percent = Math.ceil(position / total * 100);
                }
                //Set progress
                //status.setProgress(percent);
              }, false);
            }
            return xhrobj;
          },
          url: _basePath + '/FileUpload',
          type: 'POST',
          dataType: 'json',
          contentType: false,
          processData: false,
          cache: false,
          data: formData
        }).done(function (data) {
          //status.setProgress(100);
        }).fail(function (jqXHR, textStatus, errorThrown) {
          errorText += errorThrown + '\n';
        }).always(function () {
          if (--fileCount === 0) {
            _loadFolder().then(function () {
              if (errorText.length === 0) {
                resolve();
              } else {
                reject(Error(errorText));
              }
            }, reject);
          }
        });
      }
      //status.setAbort(jqXHR);
    });
  };
  this.upload = _upload;

  var _readText = function (file) {
    return new Promise(function (resolve, reject) {
      $.ajax({
        url: _basePath + '/FileReadText',
        data: $.extend({}, _baseDataObject, {
          path: file.FullPath
        }),
        type: 'POST',
        dataType: 'text'
      }).done(function (text) {
        resolve(text);
      }).fail(function (xhr, textStatus, errorThrown) {
        reject(Error('UNABLE TO LOAD SELECTED FILE\n' + errorThrown));
      });
    });
  };
  this.readText = _readText;

  var _writeText = function (file, text) {
    return new Promise(function (resolve, reject) {
      $.ajax({
        url: _basePath + '/FileWriteText',
        data: $.extend({}, _baseDataObject, {
          path: file.FullPath,
          text: text
        }),
        type: 'POST',
        dataType: 'text'
      }).done(function () {
        resolve();
      }).fail(function (xhr, textStatus, errorThrown) {
        reject(Error('UNABLE TO SAVE SELECTED FILE\n' + errorThrown));
      });
    })
  };
  this.writeText = _writeText;

  var _saveUrl = function (file, url) {
    return new Promise(function (resolve, reject) {
      $.ajax({
        url: _basePath + '/FileSaveUrl',
        data: $.extend({}, _baseDataObject, {
          path: file.FullPath,
          url: url
        }),
        type: 'POST',
        dataType: 'text'
      }).done(function () {
        resolve();
      }).fail(function (xhr, textStatus, errorThrown) {
        reject(Error('UNABLE TO SAVE SELECTED FILE\n' + errorThrown));
      });
    })
  };
  this.saveUrl = _saveUrl;

  var _loadImportableProjects = function ($select) {
    return new Promise(function (resolve, reject) {
      $.ajax({
        url: _basePath + '/GetImportableProjects',
        data: _baseDataObject,
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        $select.empty();
        if (data.Playgrounds.length > 0) {
          $select.append($('<option disabled>PLAYGROUNDS</option>'));
          for (var i = 0; i < data.Playgrounds.length; i++) {
            var proj = data.Playgrounds[i];
            $select.append($('<option>', { text: proj.Name, value: proj.RepositoryId }).data('type', proj.Type));
          }
        }
        if (data.AssignmentSubmissions.length > 0) {
          $select.append($('<option disabled>ASSIGNMENT SUBMISSIONS</option>'));
          for (var i = 0; i < data.AssignmentSubmissions.length; i++) {
            var proj = data.AssignmentSubmissions[i];
            $select.append($('<option>', { text: proj.Name, value: proj.RepositoryId }).data('type', proj.Type));
          }
        }
        if (data.AssignmentWorkspaces.length > 0) {
          $select.append($('<option disabled>ASSIGNMENT WORKSPACES</option>'));
          for (var i = 0; i < data.AssignmentWorkspaces.length; i++) {
            var proj = data.AssignmentWorkspaces[i];
            $select.append($('<option>', { text: proj.Name, value: proj.RepositoryId }).data('type', proj.Type));
          }
        }
        if ($select.find('option').length > 0) {
          resolve();
        } else {
          reject(Error('Sorry. There are no projects available to import at this time.'));
        }
      }).fail(function (xhr, textStatus, errorThrown) {
        reject(Error('UNABLE TO LOAD SELECTED FILE\n' + errorThrown));
      });
    });
  };
  this.loadImportableProjects = _loadImportableProjects;

  var _importProject = function (type, repositoryId) {
    return new Promise(function (resolve, reject) {
      _folderChanging();
      $.post({
        url: _basePath + '/ImportProject',
        data: $.extend({}, _baseDataObject, {
          sourceType: type,
          sourceRepositoryId: repositoryId
        }),
        type: 'POST',
        dataType: 'json'
      }).done(function (data) {
        _loadFolder().then(resolve,reject);
      }).fail(function (jqXHR, textStatus, errorThrown) {
        _folderChanged();
        reject(Error(errorThrown));
      });
    });
  };
  this.importProject = _importProject;

}; // END fileManager class
