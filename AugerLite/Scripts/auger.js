if (typeof window.Auger === 'undefined') {

    window.Auger = {
        _pageName: "",
        _pageSource: "",
        _styleSheets: [],
        _pingMessage: "PONG",
        _augerUrlBase: "",
        _scriptUrlBase: "",
        _contentUrlBase: "",
        _gui: true,
        Ready: false
    };
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

        urlparts = src.split('?');
        if (urlparts.length > 1 && urlparts[1] === "nogui") {
            Auger._gui = false;
        }
        if (/PhantomJS/.test(window.navigator.userAgent)) {
            Auger._gui = false;
        }
    }

    Auger.Init = function (callback) {
        "use strict";
        function _loadDependencies(innerCallback) {
            if (typeof jQuery === 'undefined') {
                Auger.LoadScript(Auger._scriptUrlBase + 'jquery.js',
                    function () { _loadDependencies(innerCallback); });
                return;
            }
            if (Auger._gui && typeof $.fn.modal === 'undefined') {
                Auger.LoadScript(Auger._scriptUrlBase + 'bootstrap.min.js',
                    function () { _loadDependencies(innerCallback); });
                return;
            }
            if (typeof URI === 'undefined') {
                Auger.LoadScript(Auger._scriptUrlBase + 'URI.js',
                    function () { _loadDependencies(innerCallback); });
                return;
            }
            if (typeof Mocha === 'undefined') {
                Auger.LoadScript(Auger._scriptUrlBase + "mocha.js",
                    function () { _loadDependencies(innerCallback); });
                return;
            }
            if (typeof chai === 'undefined') {
                Auger.LoadScript(Auger._scriptUrlBase + "chai.js",
                    function () {
                        Auger.LoadScript(Auger._scriptUrlBase + "chai-jquery.js",
                            function () { _loadDependencies(innerCallback); });
                    });
                return;
            }
            innerCallback();
        }

        function _done() {
            window.expect = chai.expect;
            window.should = chai.should();
            mocha.setup({ ui: 'bdd', reporter: Auger.JSONReporter });
            Auger.Ready = true;
            callback();
        }

        if (Auger.Ready) {
            callback();
        } else {
            _loadDependencies(_done);
        }
    };

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

    var emptyTestResults = {
        stats: { suites: 0, tests: 0, passes: 0, pending: 0, failures: 0 },
        tests: [],
        passes: [],
        pending: [],
        failures: []
    };

    Auger.RunTest = function (url, callback) {
        // run inside of an onload event to insure that everything else is done
        $(function () {
            // HACK: Remove the auger footer if the GUI is loaded
            var $augerFooter = $('#auger-footer');
            if ($augerFooter.length > 0) {
                $augerFooter.remove();
            }

            // clear out the suites and tests so that we ONLY run the new ones
            mocha.suite.suites.splice(0, mocha.suite.suites.length);
            mocha.suite.tests.splice(0, mocha.suite.tests.length);

            // load and run this test suite
            $.getScript(url, function () {
                //mocha.checkLeaks();
                if (mocha.suite.total() > 0) {
                    var runner = mocha.run();
                    runner.on('end', function () {
                        returnResults(runner.testResults);
                    });
                } else {
                    returnResults(emptyTestResults);
                }
            }).fail(function (jqxhr, settings, exception) {
                returnResults(emptyTestResults);
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
                    callback(runner.testResults);
                });
            } else {
                callback(emptyTestResults);
            }
        });
    };

    Auger.JSONReporter = function (runner) {
        "use strict";
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

    Auger.Rgb2Hex = function (rgb) {
        "use strict";
        var rgb = rgb.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
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
        var hex = hex.replace('#', '');
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

    Auger._getImage = function (elem, src, callback) {
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

    Auger.GetImage = function (imageElement, callback) {
        "use strict";
        var src = $(imageElement).attr('src');
        Auger._getImage($(imageElement)[0], src, callback);
    }

    Auger.GetCssImage = function (element, callback) {
        "use strict";
        var src = $(element).css('background-image');
        if (!!src) {
            src = src.replace(/url\(|\)|"|'/g, '');
        }
        Auger._getImage($(element)[0], src, callback);
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

    Auger.GetAsRelative = function (url) {
        var pageUri = URI(window.location.href);
        return URI(url).relativeTo(pageUri);
    }

    Auger.GetIsRelative = function (url) {
        return url.toString().indexOf("//") == -1;
    }

    Auger.GetIsFile = function (url) {
        return url.toString().indexOf("file://") != -1;
    }

    Auger.GetIsLocal = function (url) {
        return url.toString().indexOf("file://") != -1 ||
               url.toString().indexOf("//localhost") != -1 ||
               url.toString().indexOf("//127.0.0.1") != -1;
    }

    Auger.Ping = function () {
        return this._pingMessage;
    }

    if (Auger._gui) {

        Auger.TestPage = function () {
            "use strict";
            Auger.IsLoggedIn(function (isLoggedIn) {
                var $modal = Auger.GetModal('Test Page');
                $modal.on('hidden.bs.modal', function () {
                    $(this).data('bs.modal', null);
                    $modal.remove();
                });
                var $testButton = $('<button type="button" class="btn btn-primary btn-sm"/>')
                $testButton.append('<i class="fa fa-check"></i> Test Page');
                $testButton.attr('disabled', 'disabled');

                $modal.footer.append($testButton);
                $modal.footer.append('<button type="button" class="btn btn-default btn-sm" data-dismiss="modal">Close</button>');

                if (!isLoggedIn) {
                    $modal.body.append('<h3 class="has-error">ERROR</h3>');
                    $modal.body.append('<p class="has-error">You must be logged in to test your page. Click the button below, sign in, and try again.</p>');
                    var $anchor = $('<a class="btn btn-primary btn-sm" target="_blank" />')
                        .attr('href', Auger._augerUrlBase + 'Assignment/')
                        .html("Login to Auger")
                        .click(function () { $modal.modal('hide'); });
                    $modal.body.append($('<p/>').append($anchor));
                    $modal.modal();
                } else {
                    Auger.GetAssignments(function (assignments) {
                        if (assignments && assignments.length > 0) {

                            var $pageName = $('<input class="form-control input-sm" type="text" name="page-name" id="page-name" disabled/>');
                            $pageName.val(Auger._pageName);
                            var $pageNotFound = $('<div class="help-block has-error"/>')
                                .html('ERROR: This Page Was Not Found for the Selected Assignment')
                                .hide();

                            var $assignments = $('<select id="assignment-select" name="assignment-select" class="form-control input-sm" />');
                            for (var i = 0; i < assignments.length; i++) {
                                $assignments.append(new Option(assignments[i].AssignmentName, assignments[i].AssignmentId));
                            }
                            var selectedAssignment = {};
                            $assignments.change(function () {
                                var selectedIndex = $assignments.find('option:selected').index();
                                selectedAssignment = assignments[selectedIndex];
                                var pageExists = false;
                                for (var i = 0; i < selectedAssignment.Pages.length; i++) {
                                    if (selectedAssignment.Pages[i].PageName.toLowerCase() == Auger._pageName.toLowerCase()) {
                                        pageExists = true;
                                    }
                                }
                                if (pageExists) {
                                    $pageNotFound.hide();
                                    $testButton.removeAttr('disabled');
                                } else {
                                    $pageNotFound.show();
                                    $testButton.attr('disabled', 'disabled');
                                }
                            });

                            $testButton.click(function () {
                                $modal.on('hidden.bs.modal', function () {
                                    $(this).data('bs.modal', null);
                                    $modal.remove();

                                    var isFile = Auger.GetIsFile(window.location.href);
                                    var isLocal = Auger.GetIsLocal(window.location.href);

                                    var script = Auger._augerUrlBase +
                                        'PreSubmission/GetScript?' +
                                        'assignmentId=' + selectedAssignment.AssignmentId + '&' +
                                        'pageName=' + Auger._pageName + '&' +
                                        'viewportWidth=' + window.innerWidth;

                                    // if isFile Only Test Script since can't get HTML + CSS text
                                    // otherwise ValidateHTML, ValidateCSS & Test Script
                                    Auger.RunTest(script, function (results) {

                                        $modal = Auger.GetModal('Test Results');
                                        $modal.on('hidden.bs.modal', function () {
                                            $(this).data('bs.modal', null);
                                            $modal.remove();
                                        });
                                        $modal.footer.append('<button type="button" class="btn btn-default btn-sm" data-dismiss="modal">Close</button>');
                                        $modal.modal();

                                        Auger.AppendDomTestResults($modal.body, results);

                                        if (!isFile) {
                                            Auger.ValidateHTML(function (results) {
                                                Auger.AppendHtmlTestResults($modal.body, results);

                                                var errorCount = 0;
                                                for (var i = 0; i < Auger._styleSheets.length; i++) {
                                                    var fileName = Auger.GetAsRelative(Auger._styleSheets[i].href);
                                                    Auger.ValidateCSS(i, function (results) {
                                                        errorCount = Auger.AppendCSSTestResults($modal.body, fileName, results, errorCount);
                                                        Auger.ResizeModal();
                                                    });
                                                }
                                            });
                                        } else {
                                            Auger.AppendValidationWarning($modal.body);
                                        }
                                    });
                                });
                                $modal.modal('hide');
                            });

                            $modal.body.append(
                                $('<div class="form-group"/>')
                                    .append('<label for="assignment-select">Assignment:</label>')
                                    .append($assignments)
                            );
                            $modal.body.append(
                                $('<div class="form-group"/>')
                                    .append('<label for="page-name">Page:</label>')
                                    .append($pageName)
                            );

                            $modal.body.append($pageNotFound);
                            $assignments.change();

                        } else {
                            $modal.body.append('<h3>ERROR</h3>');
                            $modal.body.append('<p>It appears that you do not have any open assignments to test.</p>');
                        }
                        $modal.modal();
                    })
                }
            });
        }

        Auger.GetModal = function (title) {
            "use strict";
            var $modal = $('<div class="modal fade auger-modal" role="dialog"/>');
            $modal.dlg = $('<div class="modal-dialog"/>');
            $modal.content = $('<div class="modal-content"/>');
            $modal.header = $('<div class="modal-header"/>');
            $modal.body = $('<div class="modal-body"/>');
            $modal.footer = $('<div class="modal-footer"/>');
            $modal.append($modal.dlg.append($modal.content.append($modal.header).append($modal.body).append($modal.footer)));

            if (typeof title === 'string' && title.length > 0) {
                $modal.header.append('<button type="button" class="close" data-dismiss="modal">&times;</button>' +
                                     '<h4 class="modal-title">' + title + '</h4>');
            }
            return $modal;
        }

        Auger.ResizeModal = function () {
            "use strict";
            var mdMargin = parseInt($('.modal .modal-dialog').css('margin-top'));
            var mhHeight = $('.modal .modal-header').outerHeight();
            var mfHeight = $('.modal .modal-footer').outerHeight();
            var maxHeight = $(window).height() - mhHeight - mfHeight - (2 * mdMargin);
            $('.modal .modal-body').css({ "overflow-y": "auto", "max-height": maxHeight });
        }

        Auger.IsLoggedIn = function (callback) {
            "use strict";
            $.ajax({
                url: Auger._augerUrlBase + 'PreSubmission/TestLogin',
                type: 'GET',
                dataType: 'jsonp',
                contentType: "text/javascript",
                jsonp: 'callback'
            }).done(function (data) {
                callback(data);
            }).fail(function (xhr) {
                alert(JSON.stringify(xhr));
                callback('error');
            });
        }

        Auger.GetAssignments = function (callback) {
            "use strict";
            $.ajax({
                url: Auger._augerUrlBase + 'PreSubmission/GetAllAssignments',
                type: 'GET',
                dataType: 'jsonp',
                contentType: "text/javascript",
                jsonp: 'callback'
            }).done(function (data) {
                callback(data);
            }).fail(function (xhr) {
                alert(JSON.stringify(xhr));
                callback('error');
            });
        }

        Auger.ValidateHTML = function (callback) {
            "use strict";
            $.ajax({
                url: Auger._augerUrlBase + 'PreSubmission/ValidateHTML',
                data: {
                    fileName: Auger._pageName,
                    text: Auger._pageSource
                },
                type: 'POST',
                dataType: 'json'
            }).done(function (data) {
                callback(data);
            }).fail(function (xhr) {
                alert(JSON.stringify(xhr));
                callback('error');
            });
        }

        Auger.ValidateCSS = function (cssIndex, callback) {
            "use strict";
            var fileName = Auger.GetAsRelative(Auger._styleSheets[cssIndex].href).toString();
            $.ajax({
                url: Auger._augerUrlBase + 'PreSubmission/ValidateCSS',
                data: {
                    fileName: fileName,
                    text: Auger._styleSheets[cssIndex].source
                },
                type: 'POST',
                dataType: 'json'
            }).done(function (data) {
                callback(data);
            }).fail(function (xhr) {
                alert(JSON.stringify(xhr));
                callback('error');
            });
        }

        Auger.AppendDomTestResults = function ($div, results) {
            var errCount = results.stats.failures;

            var $innerdiv = $div.find('.auger-dom-errors');
            if ($innerdiv.length > 0) {
                $innerdiv.html('');
            } else {
                $innerdiv = $('<div class="auger-dom-errors" />');
            }
            $div.append($innerdiv);

            if (errCount == 0) {
                $innerdiv.append(
                    $('<h2/>').html('<i class="fa fa-check-circle"></i> No Pregrade Assignment Check Errors')
                ).append(
                    $('<p/>').html('Remember that this simply means that the assignment passes a basic "sanity-check". It does not mean that your assignment is 100% correct.')
                );
            } else {
                var $msgs = $('<ul/>');
                var msgs = results.failures;

                for (var i = 0; i < msgs.length; i++) {
                    var msg = msgs[i];
                    var $li = $('<li/>').text(msg.title);
                    $msgs.append($li);
                }
                $innerdiv.append(
                    $('<h2/>').addClass('has-error').html('<i class="fa fa-exclamation-circle"></i> Pregrade Assignment Check Errors (' + errCount + ')')
                );
                $innerdiv.append($msgs);
            }

            return errCount;
        }

        Auger.AppendValidationWarning = function ($div) {
            var $innerdiv = $div.find('.auger-validation-warning');
            if ($innerdiv.length == 0) {
                $innerdiv = $('<div class="auger-validation-warning" />');
                $innerdiv.append(
                    $('<h2/>').html('<i class="fa fa-question-circle"></i> Validation Warning')
                ).append(
                    $('<p/>').html(
                        'Auger was unable to perform HTML or CSS validation on your page. ' +
                        'You may wish to perform this task manually.'
                    )
                ).append(
                    $('<ul/>').append(
                        $('<li/>').html('<a href="https://validator.w3.org/" target="_blank">W3C HTML Validation Service</a>')
                    ).append(
                        $('<li/>').html('<a href="https://validator.w3.org/nu/" target="_blank">Alternate HTML Validation Service</a>')
                    ).append(
                        $('<li/>').html('<a href="https://jigsaw.w3.org/css-validator/" target="_blank">W3C CSS Validation Service</a>')
                    )
                );
            }
            $div.append($innerdiv);
        }

        Auger.AppendHtmlTestResults = function ($div, results) {
            var $innerdiv = $div.find('.auger-html-errors');
            if ($innerdiv.length > 0) {
                $innerdiv.html('');
            } else {
                $innerdiv = $('<div class="auger-html-errors" />');
            }
            $div.append($innerdiv);

            var completed = results.HtmlValidationCompleted;

            if (completed == true) {
                var $msgs = $('<ul/>');
                var msgs = results.W3CHtmlValidationMessages;

                var errCount = 0;
                for (var i = 0; i < msgs.length; i++) {
                    var msg = msgs[i];
                    //if (msg.Type === 'Error') {
                    if (msg.Type == 2) {
                        errCount++;
                        var $li = $('<li/>').html('line ' + msg.LastLine + ': ' + msg.Message);
                        if (msg.Extract.length > 0) {
                            var $ext = $('<div class="auger-extract"/>');
                            var text = msg.Extract;
                            var start = msg.HiliteStart;
                            var end = start + msg.HiliteLength;
                            text = text.slice(0, start) + '(((HS)))' + text.slice(start, end) + '(((HE)))' + text.slice(end);
                            text = Auger.EscapeHTML(text);
                            text = text.replace('(((HS)))', '<span class="auger-hilite">');
                            text = text.replace('(((HE)))', '</span>');
                            $ext.append(text);
                            $li.append($ext);
                        }
                        $msgs.append($li);
                    }
                }
                if (errCount > 0) {
                    $innerdiv.append(
                        $('<h2/>').addClass('has-error').html('<i class="fa fa-exclamation-circle"></i> HTML Validation Errors (' + errCount + ')')
                    );
                    $innerdiv.append($msgs);
                } else {
                    $innerdiv.append(
                        $('<h2/>').html('<i class="fa fa-check-circle"></i> No HTML Validation Errors')
                    );
                }
                return errCount;
            } else {
                $innerdiv.append(
                    $('<h2/>').html('<i class="fa fa-question-circle"></i> Validation Warning')
                ).append(
                    $('<p/>').html(
                        'Auger was unable to perform HTML validation on your page. ' +
                        'You may wish to perform this task manually.'
                    )
                ).append(
                    $('<ul/>').append(
                        $('<li/>').html('<a href="https://validator.w3.org/" target="_blank">W3C HTML Validation Service</a>')
                    ).append(
                        $('<li/>').html('<a href="https://validator.w3.org/nu/" target="_blank">Alternate HTML Validation Service</a>')
                    )
                );
                return 0;
            }
        }

        Auger.AppendCSSTestResults = function ($div, fileName, results, totalErrCount) {
            var $innerdiv = $div.find('.auger-css-errors');
            if ($innerdiv.length > 0 && errCount == 0) {
                $innerdiv.html('');
                $innerdiv.append(
                    $('<h2/>').html('<i class="fa fa-check-circle"></i> No CSS Validation Errors')
                );
            } else if ($innerdiv.length == 0) {
                $innerdiv = $('<div class="auger-css-errors" />');
                $innerdiv.append(
                    $('<h2/>').html('<i class="fa fa-check-circle"></i> No CSS Validation Errors')
                );
            }
            $div.append($innerdiv);

            var completed = results.CssValidationCompleted;
            var errCount = 0;

            if (completed == true) {

                var $msgs = $('<ul/>');
                var msgs = results.W3CCssValidationMessages;

                var fileName = '';
                for (var i = 0; i < msgs.length; i++) {
                    var msg = msgs[i];
                    //if (msg.Level === 'Error') {
                    if (msg.Level == 2) {
                        errCount++;
                        totalErrCount++;
                        var $li = $('<li/>').html('line ' + msg.Line + ': ' + msg.Message);
                        if (msg.Context.length > 0) {
                            $li.append(' <span class="auger-context">(in ' + msg.Context + ' selector)</span>');
                        }
                        $msgs.append($li);
                    }
                }
            } else {
                totalErrCount++;
            }
            if (errCount > 0 || !completed) {
                $innerdiv.find('h2').addClass('has-error').html('<i class="fa fa-exclamation-circle"></i> CSS Validation Errors (' + totalErrCount + ')');
                $innerdiv.append('<h3><i class="fa fa-file-code-o"></i> ' + fileName + '</h3>');
                if (errCount > 0) {
                    $innerdiv.append($msgs);
                } else {
                    $innerdiv.append(
                        $('<p/>').html(
                            'Auger was unable to perform CSS validation on your file. ' +
                            'You may wish to perform this task manually.'
                        )
                    ).append(
                        $('<ul/>').append(
                            $('<li/>').html('<a href="https://jigsaw.w3.org/css-validator/" target="_blank">W3C CSS Validation Service</a>')
                        )
                    );
                }
            }
            return totalErrCount;
        }

        Auger.SubmitAssignment = function () {
            // TODO: Do the assignment submission function (just redirect to
            // main submission page, but pass the URL if isLocal is false).
        }

        var InitGUI = function () {
            // Initialize Auger
            Auger.Init(function () {
                $(function () {
                    var isFile = Auger.GetIsFile(window.location.href);
                    var isLocal = Auger.GetIsLocal(window.location.href);
                    if (!isFile) {
                        Auger.GetFile(window.location.href, function (source) {
                            Auger._pageSource = source;
                        });
                        var links = document.getElementsByTagName('link');
                        for (var i = 0; i < links.length; i++) {
                            var link = links[i];
                            if (link.rel === "stylesheet") {
                                if (Auger.GetIsRelative(Auger.GetAsRelative(link.href))) {
                                    Auger.GetFile(link.href, function (source, href) {
                                        Auger._styleSheets.push({ href: href, source: source });
                                    });
                                }
                            }
                        }
                    }

                    var $augerDiv =
                        $('<div id="auger-footer"/>').append(
                            $('<div id="auger-title"/>')
                        ).append(
                            $('<div id="auger-body"/>')
                        ).appendTo('body').hide();

                    var $augerTitle = $('#auger-title');
                    var $augerBody = $('#auger-body');

                    $augerBody.hide();

                    $augerTitle.append($('<i class="fa fa-bars"></i>')).append('&nbsp;Assignment Menu');

                    $augerBody.append($('<p/>').html(
                        'Click to check your page prior to submitting it. Note that passing these tests ' +
                        'does not insure a perfect score for the assignment.'
                    ));

                    $augerBody.append(
                        $('<div/>').append(
                            $('<button class="btn btn-primary" id="auger-test-page" />').html(
                                '<i class="fa fa-check"></i> Test Page'
                            )
                        )
                    );

                    var message = 'When you are finished with your site, you may click the following button to submit it.';
                    if (isLocal) {
                        message += " Note that you may need to save your site's folder as a ZIP file first."
                    }

                    $augerBody.append($('<hr/>'));

                    $augerBody.append($('<p/>').html(message));

                    $augerBody.append(
                        $('<div/>').append(
                            $('<a class="btn btn-danger" target="_blank" id="auger-submit-assignment" />')
                                .attr('href', Auger._augerUrlBase + '/Assignment/')
                                .append($('<i class="fa fa-cloud-upload"/>'))
                                .append(' Submit Assignment')
                        )
                    );

                    $(window).resize(Auger.ResizeModal);

                    $(document).on('click', '#auger-title', function (evt) {
                        $augerBody.slideToggle(250);
                        evt.stopPropagation();
                    });
                    $(document).on('click', '#auger-body', function (evt) {
                        evt.stopPropagation();
                    });
                    $(document).on('click', '#auger-test-page', Auger.TestPage);
                    //$(document).on('click', '#auger-submit-assignment', Auger.SubmitAssignment);
                    $(document).click(function () {
                        if ($augerBody.is(':visible')) {
                            $augerBody.slideUp(250);
                        }
                    });
                    $('head').append(
                        $('<link/>', {
                            rel: 'stylesheet',
                            type: 'text/css',
                            href: Auger._contentUrlBase + 'auger-footer.css'
                        })
                    ).append(
                        $('<link/>', {
                            rel: 'stylesheet',
                            type: 'text/css',
                            href: Auger._contentUrlBase + 'font-awesome.min.css'
                        })
                    ).append(
                        $('<link/>', {
                            rel: 'stylesheet',
                            type: 'text/css',
                            href: Auger._contentUrlBase + 'bootstripped.css'
                        })
                    ).append(
                        $('<link/>', {
                            rel: 'stylesheet',
                            type: 'text/css',
                            href: 'https://fonts.googleapis.com/css?family=Roboto'
                        })
                    );

                    $augerDiv.fadeIn(250);

                });
            });
        };

        if (window.addEventListener) {
            window.addEventListener('DOMContentLoaded', InitGUI, false);
        } else {
            window.attachEvent('onload', InitGUI);
        }

    }
}
