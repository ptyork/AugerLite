/// <binding BeforeBuild='copy' />
/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. https://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
  var modernizrTests = [
    "contextmenu",
    "cookies",
    "cors",
    "customevent",
    "hiddenscroll",
    "ie8compat",
    "inputtypes",
    [
      "progressbar",
      "meter"
    ],
    "promises",
    "filereader",
    "filesystem",
    "fileinput",
    "sandbox",
    "seamless",
    "srcdoc",
    "urlparser"
  ];
  var modernizrOptions = [
    "setClasses"
  ];
  grunt.initConfig({
    modernizr: {
      dev: {
        "dest": "lib/modernizr/modernizr.js",
        "crawl": false,
        "customTests": [],
        "tests": modernizrTests,
        "options": modernizrOptions,
        "uglify": false
      },
      dist: {
        "dest": "lib/modernizr/modernizr.min.js",
        "crawl": false,
        "customTests": [],
        "tests": modernizrTests,
        "options": modernizrOptions,
        "uglify": true
      }
    },
    copy: {
      bower: {
        files: [
          {
            cwd: 'bower_components/', src: [
              'chai/chai.js',
              'chai-jquery/chai-jquery.js',
              'es6-promise/*.js',
              'jquery-validation-unobtrusive/*js',
              'mocha/mocha.js',
              'mustache.js/*.js',
              'srcdoc-polyfill/srcdoc*.js',
            ], dest: 'lib/', expand: true
          },
          { cwd: 'bower_components/ace-builds/src-min/', src: '**', dest: 'lib/ace/', expand: true },
          { cwd: 'bower_components/bootstrap/dist/', src: '**', dest: 'lib/bootstrap/', expand: true },
          { cwd: 'bower_components/bootstrap-switch/dist/', src: '**', dest: 'lib/bootstrap-switch/', expand: true },
          { cwd: 'bower_components/eonasdan-bootstrap-datetimepicker/build/', src: '**', dest: 'lib/datetimepicker/', expand: true },
          { cwd: 'bower_components/eonasdan-bootstrap-datetimepicker/src/js', src: 'bootstrap-datetimepicker.js', dest: 'lib/datetimepicker/js/', expand: true },
          { cwd: 'bower_components/font-awesome/', src: ['css/**', 'fonts/**'], dest: 'lib/font-awesome/', expand: true },
          { cwd: 'bower_components/jquery/dist/', src: '**', dest: 'lib/jquery/', expand: true },
          { cwd: 'bower_components/jquery-validation/dist/', src: '**', dest: 'lib/jquery-validation/', expand: true },
          { cwd: 'bower_components/moment/min/', src: '*.js', dest: 'lib/moment/', expand: true },
          { cwd: 'bower_components/moment/', src: '*.js', dest: 'lib/moment/', expand: true },
          { cwd: 'bower_components/moment-timezone/builds/', src: '*.js', dest: 'lib/moment-timezone/', expand: true },
          { cwd: 'bower_components/moment-timezone/', src: '*.js', dest: 'lib/moment-timezone/', expand: true },
          { cwd: 'bower_components/respond/dest/', src: '*.js', dest: 'lib/respond/', expand: true },
        ]
        //src: [
        //  'ace-builds/**',
        //  'bootstrap/dist/**',
        //  'bootstrap-switch/dist/**',
        //  'chai/chai.js',
        //  'chai-jquery/chai-jquery.js'

        //]
      }
    }
  });
  grunt.loadNpmTasks("grunt-modernizr");
  grunt.loadNpmTasks("grunt-contrib-copy");
};