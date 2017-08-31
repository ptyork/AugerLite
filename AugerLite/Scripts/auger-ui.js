/*
 * on and bind extensions
 * from: https://stackoverflow.com/questions/4298612
 */
(function ($) {
  var methods = { on: $.fn.on, bind: $.fn.bind };
  $.each(methods, function (k) {
    $.fn[k] = function () {
      var args = [].slice.call(arguments),
        delay = args.pop(),
        fn = args.pop(),
        timer;

      args.push(function () {
        var self = this,
          arg = arguments;
        clearTimeout(timer);
        timer = setTimeout(function () {
          fn.apply(self, [].slice.call(arg));
        }, delay);
      });

      return methods[k].apply(this, isNaN(delay) ? arguments : args);
    };
  });
}(window.jQuery));


// From https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Object/keys
if (!Object.keys) {
  Object.keys = (function () {
    'use strict';
    var hasOwnProperty = Object.prototype.hasOwnProperty,
      hasDontEnumBug = !({ toString: null }).propertyIsEnumerable('toString'),
      dontEnums = [
        'toString',
        'toLocaleString',
        'valueOf',
        'hasOwnProperty',
        'isPrototypeOf',
        'propertyIsEnumerable',
        'constructor'
      ],
      dontEnumsLength = dontEnums.length;

    return function (obj) {
      if (typeof obj !== 'function' && (typeof obj !== 'object' || obj === null)) {
        throw new TypeError('Object.keys called on non-object');
      }

      var result = [], prop, i;

      for (prop in obj) {
        if (hasOwnProperty.call(obj, prop)) {
          result.push(prop);
        }
      }

      if (hasDontEnumBug) {
        for (i = 0; i < dontEnumsLength; i++) {
          if (hasOwnProperty.call(obj, dontEnums[i])) {
            result.push(dontEnums[i]);
          }
        }
      }
      return result;
    };
  }());
}

/*! https://mths.be/startswith v0.2.0 by @mathias */
if (!String.prototype.startsWith) {
  (function () {
    'use strict'; // needed to support `apply`/`call` with `undefined`/`null`
    var defineProperty = (function () {
      // IE 8 only supports `Object.defineProperty` on DOM elements
      try {
        var object = {};
        var $defineProperty = Object.defineProperty;
        var result = $defineProperty(object, object, object) && $defineProperty;
      } catch (error) { }
      return result;
    }());
    var toString = {}.toString;
    var startsWith = function (search) {
      if (this == null) {
        throw TypeError();
      }
      var string = String(this);
      if (search && toString.call(search) == '[object RegExp]') {
        throw TypeError();
      }
      var stringLength = string.length;
      var searchString = String(search);
      var searchLength = searchString.length;
      var position = arguments.length > 1 ? arguments[1] : undefined;
      // `ToInteger`
      var pos = position ? Number(position) : 0;
      if (pos != pos) { // better `isNaN`
        pos = 0;
      }
      var start = Math.min(Math.max(pos, 0), stringLength);
      // Avoid the `indexOf` call if no match is possible
      if (searchLength + start > stringLength) {
        return false;
      }
      var index = -1;
      while (++index < searchLength) {
        if (string.charCodeAt(start + index) != searchString.charCodeAt(index)) {
          return false;
        }
      }
      return true;
    };
    if (defineProperty) {
      defineProperty(String.prototype, 'startsWith', {
        'value': startsWith,
        'configurable': true,
        'writable': true
      });
    } else {
      String.prototype.startsWith = startsWith;
    }
  }());
}



var safeCall = function (func) {
  if (typeof func !== 'function') {
    func = function () { };
  }
  var args = [];
  for (var i = 1; i < arguments.length; i++) {
    args.push(arguments[i]);
  }
  func.apply(args);
};

var isValidFilename = function (name, extensions) {
  if (!extensions) {
    return /^[\w][\w\-\.]*[^.]$/.test(name);
  } else {
    return RegExp('^[\\w][\\w\\-\\.]*\\.(' + extensions + ')$', 'i').test(name);
  }
}

function createGuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    var r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

function htmlEncode(value) {
  return $('<div/>').text(value).html();
}
function htmlDecode(value) {
  return $('<div/>').html(value).text();
}

function showLoadingDiv($elem) {
  if ($elem.find('.loading').length === 0) {
    var $div = $('<div class="loading"></div>');
    var $spinner = $('<i class="fa fa-spinner fa-pulse fa-5x fa-fw"></i>');
    var $text = $('<div>LOADING<br>PLEASE WAIT</div>');
    $div.append($spinner);
    $div.append($text);
    $elem.append($div);
  }
}
function hideLoadingDiv($elem) {
  $elem.find('.loading').remove();
}

function resizeFullHeightElements() {
  $('.full-height').each(function () {
    $(this).height($(window).innerHeight() - this.getBoundingClientRect().top);
  });
}
$(window).on('resize', function () {
  resizeFullHeightElements();
}, 50);
$(function () {
  resizeFullHeightElements();
});

function setInputSelection(input, startPos, endPos) {
  input = $(input)[0]; // work for both DOM and jQuery element
  input.focus();
  if (typeof input.selectionStart != "undefined") {
    input.selectionStart = startPos;
    input.selectionEnd = endPos;
  } else if (document.selection && document.selection.createRange) {
    // IE branch
    input.select();
    var range = document.selection.createRange();
    range.collapse(true);
    range.moveEnd("character", endPos);
    range.moveStart("character", startPos);
    range.select();
  }
}

/* 
 * Context.js
 * Copyright Jacob Kelley (modified by Paul York)
 * MIT License
 */

var context = context || (function () {

  var options = {
    fadeSpeed: 100,
    filter: function ($obj) {
      // Modify $obj, Do not return
    },
    above: 'auto',
    preventDoubleContext: true,
    compress: false
  };

  var $activeTarget = null;

  function initialize(opts) {

    options = $.extend({}, options, opts);

    $(document).on('click', 'html', function () {
      $('.dropdown-context').fadeOut(options.fadeSpeed, function () {
        $('.dropdown-context').css({ display: '' }).find('.drop-left').removeClass('drop-left');
      });
    });
    if (options.preventDoubleContext) {
      $(document).on('contextmenu', '.dropdown-context', function (e) {
        e.preventDefault();
      });
    }
    $(document).on('mouseenter', '.dropdown-submenu', function () {
      var $sub = $(this).find('.dropdown-context-sub:first'),
        subWidth = $sub.width(),
        subLeft = $sub.offset().left,
        collision = (subWidth + subLeft) > window.innerWidth;
      if (collision) {
        $sub.addClass('drop-left');
      }
    });

  }

  function updateOptions(opts) {
    options = $.extend({}, options, opts);
  }

  function buildMenu(data, id, subMenu) {
    var ulClass = (!subMenu) ? '' : ' dropdown-context-sub';
    var compressed = options.compress ? ' compressed-context' : '';
    var role = (!subMenu) ? ' role="menu"' : '';
    var $menu = $('<ul class="dropdown-menu dropdown-context' + ulClass + compressed + '" id="dropdown-' + id + '"' + role + '></ul>');
    for (var i = 0; i < data.length; i++) {
      var item = data[i];
      if (!!item.divider) {
        $menu.append('<li role="separator" class="divider"></li>');
      } else if (!!item.header) {
        $menu.append('<li class="dropdown-header">' + data[i].header + '</li>');
      } else {
        var href = (!item.href) ? '' : ' href = "' + item.href + '"';
        var linkTarget = (!item.target) ? '' : ' target="' + item.target + '"';
        var liClass = (!item.subMenu) ? '' : ' class="dropdown-submenu"';
        var $sub = $('<li' + liClass + '>');
        var $a = $('<a tabindex="-1"' + href + linkTarget + '>' + item.text + '</a>');
        $sub.append($a);
        if (!!item.action) {
          var actionID = 'event-' + id + '-' + i;
          var eventAction = item.action;
          $a.attr('id', actionID);
          $a.addClass('context-event');
          $(document).on('click', '#' + actionID, { target: 'boo' }, eventAction);
          //$(document).on('click', '#' + actionID, function (e) {
          //  alert($(this).attr('id'));
          //  eventAction($activeTarget, e);
          //});
        }
        if (!!item.subMenu) {
          var subMenuData = buildMenu(item.subMenu, id + '-' + i, true);
          $sub.append(subMenuData);
        }
        $menu.append($sub);
      }
      if (typeof options.filter === 'function') {
        options.filter($sub);
      }
    }
    return $menu;
  }

  function addContext(selector, data) {

    var d = new Date(),
        id = d.getTime(),
        $menu = buildMenu(data, id);

    $('body').append($menu);

    $(document).on('contextmenu', selector, function (e) {
      e.preventDefault();
      e.stopPropagation();

      $('.dropdown-context:not(.dropdown-context-sub)').hide();

      $activeTarget = $(this);

      $dd = $('#dropdown-' + id);
      if (typeof options.above === 'boolean' && options.above) {
        $dd.addClass('dropdown-context-up').css({
          top: e.pageY - 20 - $('#dropdown-' + id).height(),
          left: e.pageX - 13
        }).fadeIn(options.fadeSpeed);
      } else if (typeof options.above === 'string' && options.above === 'auto') {
        $dd.removeClass('dropdown-context-up');
        var autoH = $dd.height() + 12;
        if ((e.pageY + autoH) > $('html').height()) {
          $dd.addClass('dropdown-context-up').css({
            top: e.pageY - 20 - autoH,
            left: e.pageX - 13
          }).fadeIn(options.fadeSpeed);
        } else {
          $dd.css({
            top: e.pageY + 10,
            left: e.pageX - 13
          }).fadeIn(options.fadeSpeed);
        }
      }
    });
  }

  function destroyContext(selector) {
    $(document).off('contextmenu', selector).off('click', '.context-event');
  }

  return {
    init: initialize,
    settings: updateOptions,
    attach: addContext,
    destroy: destroyContext,
    activeTarget: function () { return $activeTarget; }
  };
})();
