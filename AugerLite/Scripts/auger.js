if (typeof window.Auger === 'undefined') {

  // Base Auger Object
  window.Auger = {
    _pageName: "",
    _pageSource: "",
    _augerUrlBase: "",
    _scriptUrlBase: "",
    _contentUrlBase: "",
    Ready: false
  };

  // Empty test results object to pass if test run fails
  var _emptyTestResults = {
    stats: { suites: 0, tests: 0, passes: 0, pending: 0, failures: 0 },
    tests: [],
    passes: [],
    pending: [],
    failures: [],
    debugMessages: []
  };

  // Array of debug messages to append to test results
  var _debugMessages = [];

  // Initialize files names and paths based on current document
  {
    var DEFAULT_PAGE_NAME = 'index.html';

    var href = window.location.href;
    var hrefparts = href.split('/');
    Auger._pageName = hrefparts[hrefparts.length - 1];
    if (Auger._pageName.length == 0) {
      Auger._pageName = DEFAULT_PAGE_NAME;
    }

    var scripts = document.getElementsByTagName('script');
    var src = scripts[scripts.length - 1].src; // Current script will always be last loaded
    var urlparts = src.split('/');
    var url = urlparts[0];
    for (var i = 1; i < urlparts.length - 2; i++) {
      url = url + '/' + urlparts[i];
    }
    Auger._augerUrlBase = url + '/';
    Auger._scriptUrlBase = url + '/' + urlparts[urlparts.length - 2] + '/';
    Auger._contentUrlBase = url + '/Content/';
  }

  // Auger.Init: Initialize Auger's testing environment (Mocha, Chai, etc.)
  Auger.Init = function (callback) {
    "use strict";
    function _loadDependencies(innerCallback) {
      if (typeof jQuery === 'undefined') {
        Auger.LoadScript(Auger._augerUrlBase + 'lib/jquery/jquery.min.js', function () {
          _loadDependencies(innerCallback);
        });
        return;
      }
      if (typeof Mocha === 'undefined') {
        Auger.LoadScript(Auger._augerUrlBase + "lib/mocha/mocha.js", function () {
          _loadDependencies(innerCallback);
        });
        return;
      }
      if (typeof chai === 'undefined') {
        Auger.LoadScript(Auger._augerUrlBase + "lib/chai/chai.js", function () {
          Auger.LoadScript(Auger._augerUrlBase + "lib/chai-jquery/chai-jquery.js", function () {
            _loadDependencies(innerCallback);
          });
        });
        return;
      }
      innerCallback();
    }

    var _jsonReporter = function (runner) {
      var stats = { suites: 0, tests: 0, passes: 0, pending: 0, failures: 0 }
      runner.stats = stats;
      this.runner = runner;

      var tests = this.tests = [];
      var passes = this.passes = [];
      var pending = this.pending = [];
      var failures = this.failures = [];

      var page = location.pathname.substring(location.pathname.lastIndexOf("/") + 1);

      runner.on('start', function () {
        stats.start = new Date();
      });

      runner.on('suite', function (suite) {
        suite.root || stats.suites++;
      });

      runner.on('test end', function (test) {
        tests.push(test);
        stats.tests++;
      });

      runner.on('pass', function (test) {
        if (test.duration > test.slow()) {
          test.speed = 'slow';
        } else if (test.duration > test.slow() / 2) {
          test.speed = 'medium';
        } else {
          test.speed = 'fast';
        }
        passes.push(test);
        stats.passes++;
      });

      runner.on('fail', function (test, err) {
        test.err = err;
        failures.push(test);
        stats.failures++;
      });

      runner.on('pending', function (test) {
        pending.push(test);
        stats.pending++;
      });

      runner.on('end', function () {
        stats.end = new Date();
        stats.duration = new Date() - stats.start;

        var obj = {
          stats: runner.stats,
          tests: tests.map(clean),
          passes: passes.map(clean),
          pending: pending.map(clean),
          failures: failures.map(clean)
        };
        runner.testResults = obj;
      });

      function clean(test) {
        return {
          title: test.title,
          fullTitle: test.fullTitle(),
          duration: test.duration,
          speed: test.speed,
          page: page,
          parents: parentJSON(test.parent),
          passed: test.err ? false : true,
          error: errorJSON(test.err || {})
        };

        function parentJSON(test) {
          var res = [test.title];
          var par = test.parent;
          while (!par.root) {
            res.push(par.title);
            par = par.parent;
          }
          return res;
        }

        function errorJSON(err) {
          var res = {};
          Object.getOwnPropertyNames(err).forEach(function (key) {
            if (key === "lineNumber" || key === "columnNumber" || key === "message") {
              res[key] = err[key];
            }
          }, err);
          return res;
        }
      }
    };

    function _done() {
      window.assert = chai.assert;
      window.expect = chai.expect;
      window.should = chai.should();
      mocha.setup({ ui: 'bdd', reporter: _jsonReporter });
      Auger.Ready = true;
      callback();
    }

    if (Auger.Ready) {
      callback();
    } else {
      _loadDependencies(_done);
    }
  };

  // Auger.LoadScript: Simple utility function to load a given script asynchronously
  Auger.LoadScript = function (url, callback) {
    //if (url.lastIndexOf(Auger._augerUrlBase, 0) === 0) {
    //    url = Auger._augerUrlBase + 'BrowseAny?url=' + encodeURIComponent(url);
    //}
    url = Auger._augerUrlBase + 'BrowseAny?url=' + encodeURIComponent(url);
    if (typeof jQuery === 'undefined') {
      var script = document.createElement('script');
      script.type = 'text/javascript';
      script.src = url;
      if (script.readyState) { //IE
        script.onreadystatechange = function () {
          if (script.readyState == 'loaded' || script.readyState == 'complete') {
            script.onreadystatechange = null;
            callback(script);
          }
        };
      } else { //Others
        script.onload = function () {
          callback(script);
        }
        script.onerror = function () {
          alert('ERROR loading ' + url);
        }
      }
      document.getElementsByTagName('head')[0].appendChild(script);
    } else {
      $.ajax({
        url: url,
        dataType: "script",
        cache: true,
        success: callback
      }).fail(function () {
        alert('ERROR loading ' + url);
      });;
    }
  };

  Auger.RunTest = function (url, callback) {
    // run inside of an onload event to insure that everything else is done
    $(function () {
      // HACK: Remove the auger footer if the GUI is loaded
      var $augerFooter = $('#auger-footer');
      if ($augerFooter.length > 0) {
        $augerFooter.remove();
      }

      // start with a fresh _debugMessages array
      _debugMessages = [];

      // clear out the suites and tests so that we ONLY run the new ones
      mocha.suite.suites.splice(0, mocha.suite.suites.length);
      mocha.suite.tests.splice(0, mocha.suite.tests.length);

      // load and run this test suite
      $.getScript(url, function () {
        //mocha.checkLeaks();
        if (mocha.suite.total() > 0) {
          var runner = mocha.run();
          runner.on('end', function () {
            var results = runner.testResults;
            results.debugMessages = _debugMessages;
            returnResults(results);
          });
        } else {
          returnResults(_emptyTestResults);
        }
      }).fail(function (jqxhr, settings, exception) {
        returnResults(_emptyTestResults);
      });

      function returnResults(results) {
        if ($augerFooter.length > 0) {
          $augerFooter.appendTo('body');
        }
        callback(results);
      }
    });
  };

  Auger.RunTestEmbedded = function (callback) {
    // run inside of an onload event to insure that everything else is done
    $(function () {
      //mocha.checkLeaks();
      if (mocha.suite.total() > 0) {
        var runner = mocha.run();
        runner.on('end', function () {
          var results = runner.testResults;
          results.debugMessages = _debugMessages;
          callback(results);
        });
      } else {
        callback(_emptyTestResults);
      }
    });
  };

  Auger.Debug = function (message) {
    console.log(message);
    _debugMessages.push(message);
  }

  Auger.Rgb2Hex = function (rgb) {
    "use strict";
    rgb = rgb.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
    if (rgb.indexOf('rgb') == -1) {
      return rgb;
    } else {
      rgb = rgb.match(/^rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*(\d+))?\)$/);
      var hex = function (x) {
        return ("0" + parseInt(x).toString(16)).slice(-2);
      }
      return '#' + (hex(rgb[1]) + hex(rgb[2]) + hex(rgb[3])).toUpperCase();
    }
  }

  Auger.Hex2Rgb = function (hex) {
    "use strict";
    var r, g, b;
    hex = hex.replace('#', '');
    if (hex.length == 3) {
      r = hex.substr(0, 1) + hex.substr(0, 1);
      g = hex.substr(1, 1) + hex.substr(1, 1);
      b = hex.substr(2, 1) + hex.substr(2, 1);
    } else {
      r = hex.substr(0, 2);
      g = hex.substr(2, 2);
      b = hex.substr(4, 2);
    }
    r = parseInt(r, 16);
    g = parseInt(g, 16);
    b = parseInt(b, 16);
    return 'rgb(' + r + ', ' + g + ', ' + b + ')';
  }

  Auger.Px2Pt = function (pxVal) {
    var px = parseFloat(pxVal);
    var pt = px * 72.0 / 96.0;
    return Math.round(pt);
  }

  Auger._escapeElement = document.createElement('textarea');
  Auger.EscapeHTML = function (html) {
    Auger._escapeElement.textContent = html;
    return Auger._escapeElement.innerHTML;
  }

  Auger.UnescapeHTML = function (html) {
    Auger._escapeElement.innerHTML = html;
    return Auger._escapeElement.textContent;
  }

  Auger.GetFile = function (url, callback) {
    "use strict";
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url, true);
    xhr.onreadystatechange = function () {
      if (xhr.readyState === 4 && xhr.status === 200) {
        callback(xhr.responseText, url);
      }
    }
    xhr.send();
  };

  var _getImage = function (elem, src, callback) {
    if (!!src) {
      src = src.trim();
    }
    if (!src || src.length === 0) {
      callback({
        valid: false,
        element: elem
      });
      return;
    }
    var $tmpImg = $('<img/>');
    $tmpImg.load(function () {
      callback({
        valid: true,
        src: src,
        element: elem,
        width: this.width,
        height: this.height
      });
    }).error(function () {
      callback({
        valid: false,
        src: src,
        element: elem
      });
    });
    $tmpImg.attr('src', src);
  }

  Auger.GetImage = function (element, callback) {
    "use strict";
    var elem = $(element)[0];
    var src = $(element).attr('src');
    _getImage(elem, src, callback);
  }

  Auger.GetCssImage = function (element, callback) {
    "use strict";
    var elem = $(element)[0];
    var src = $(element).css('background-image');
    if (!!src) {
      src = src.replace(/url\(|\)|"|'/g, '');
    }
    _getImage(elem, src, callback);
  }

  Auger.GetImages = function ($imageElements, callback, images) {
    "use strict";
    if (!images) {
      images = [];
    }

    if ($imageElements.length > 0) {
      var imageElement = $imageElements[0];
      $imageElements.splice(0, 1);
      Auger.GetImage(imageElement, function (imageObject) {
        images.push(imageObject);
        Auger.GetImages($imageElements, callback, images);
      });
    } else {
      callback(images);
    }
  }

  Auger.Ping = function () {
    return "PONG";
  }
}
