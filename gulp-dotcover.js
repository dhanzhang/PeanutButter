var gutil = require('gulp-util');
var es = require('event-stream');
var fs = require('fs');
var child_process = require('child_process');
var q = require('q');

var PLUGIN_NAME = 'gulp-dotcover';
var DEBUG = true;

var CWD = process.cwd();
function projectPathFor(path) {
  return [CWD, path].join('/');
}

function dotCover(options) {
  options = options || { }
  options.exec = options.exec || {};
  options.exec.dotCover = options.exec.dotCover || 'C:/Program Files (x86)/JetBrains/dotCover/v3.1/Bin/dotCover.exe';
  options.exec.nunit = options.exec.nunit || 'C:/Program Files (x86)/NUnit 2.6.4/bin/nunit-console.exe';
  options.baseFilters = options.baseFilters || '+:module=*;class=*;function=*;-:*.Tests';
  options.exclude = options.exclude || [];
  options.nunitOptions = options.nunitOptions || '/framework:net-4.5 /labels';
  options.nunitOutput = projectPathFor(options.nunitOutput || 'buildreports/nunit-result.xml');
  options.coverageReportBase = projectPathFor(options.coverageReportBase || 'buildreports/coverage');
  options.coverageOutput = projectPathFor(options.coverageOutput || 'buildreports/coveragesnapshot')
  DEBUG = options.debug || false;
  
  var mainAssemblies = [];
  var testAssemblies = [];
  
  var stream = es.through(function write(file) {
    if (!file) {
      fail(stream, 'file may not be empty or undefined');
    }
    var filePath = file.history[0];
    var parts = filePath.split('\\');
    if (parts.length === 1) {
      parts = filePath.split('/');
    }
    // only accept the one which is in the debug project output for itself
    var filePart = parts[parts.length-1];
    var projectParts = filePart.split('.');
    var projectName = projectParts.slice(0, projectParts.length-1).join('.');
    var include = (parts.indexOf('bin') > -1) &&
                  (parts.indexOf('Debug') > -1) &&
                  (parts.indexOf(projectName) > -1);
    if (include) {
      testAssemblies.push(file);
    } else if(DEBUG) {
      console.log('ignore: ' + filePath);
    }
    this.emit('data', file);
  }, function end() {
    runDotCoverWith(this, testAssemblies, options);
  }); 
  return stream;
};

function findExactExecutable(stream, options, what) {
  if (options.exec[what]) {
    var exe = trim(options.exec[what], '\\s', '"', '\'');
    if (!fs.existsSync(exe)) {
      fail(stream, 'Can\'t find executable for "' + what + '" at provided path: "' + options.exec[what] + '"');
    }
    return exe; 
  }
  fail('Auto-detection of executables not implemented yet. Please specify the exec.nunit and exec.dotCover options');
}
function findDotCover(stream, options) {
  return findExactExecutable(stream, options, 'dotCover');
}

function findNunit(stream, options) {
  return findExactExecutable(stream, options, 'nunit');
}

function fail(stream, msg) {
	stream.emit('error', new gutil.PluginError(PLUGIN_NAME, msg));
}
function end(stream) {
	stream.emit('end');
}
function trim() {
	var args = Array.prototype.slice.call(arguments)
	var source = args[0];
	var replacements = args.slice(1).join(',');
	var regex = new RegExp("^[" + replacements + "]+|[" + replacements + "]+$", "g");
	return source.replace(regex, '');
}

function runDotCoverWith(stream, testAssemblies, options) {
  var assemblies = testAssemblies.map(function(file) {
    return file.path.replace(/\\/g, '/');
  });
  if (assemblies.length === 0) {
    return fail(stream, 'No test assemblies defined');
  }
  var dotCover = findDotCover(stream, options);
  var nunit = findNunit(stream, options);
  
  var filterJoin = ';-:';
  var filters = options.baseFilters;
  if (options.exclude.length) {
    filters = [filters, options.exclude.join(filterJoin)].join(filterJoin);
  }

  var nunitOptions = [ options.nunitOptions,
                       '/xml=' + options.nunitOutput,
                       '/noshadow',
                       assemblies.join(' ')].join(' ');
  var dotCoverOptions = ['cover',
                          //'/TargetWorkingDir="' + process.cwd().replace(/\\/g, '/') + '"',
                          '/TargetExecutable=' + nunit,
                          '/AnalyseTargetArguments=False',
                          '/Output=' + options.coverageOutput,
                          '/Filters=' + filters,
                          '/TargetArguments=' + nunitOptions 
                          ];
  var puts = function(str) {
    gutil.log(gutil.colors.yellow(str));
  };
  var reportArgsFor = function(reportType) {
    return ['report', 
            '/ReportType=' + reportType, 
            '/Source=' + options.coverageOutput,
            '/Output=' + options.coverageReportBase + '.' + reportType.toLowerCase()];
  }
	var opts = {
		stdio: [process.stdin, process.stdout, process.stderr, 'pipe'],
    cwd: process.cwd()
	};


  puts('running testing with coverage...');
  run('coverage', dotCover, dotCoverOptions, opts).then(function() {
    puts('creating XML report');
    var args = reportArgsFor('XML');
    return run('xml report generation', dotCover, args, opts);
  }).then(function() {
    puts('creating HTML report');
    var args = reportArgsFor('HTML');
    return run('html report generation', dotCover, args, opts);
  }).then(function() {
    end(stream)
  }).catch(function(data) {
    var message = data.name + ' failed';
    gutil.log(gutil.colors.red(message));
    gutil.log('result: ' + (data.error || data.exitCode));
    fail(stream, message);
  });
}

function run(name, executable, args, opts) {
  var deferred = q.defer();
  if (DEBUG) {
    var cmd = ['"' + executable + '"', args.join(' ')].join(' ');
    console.log(cmd);
  }
  var child = child_process.spawn(executable, args, opts);
  child.on('error', function(err) {
    deferred.reject({
      name: name,
      error: err
    });
  })
  child.on('close', function(code) {
    if (code === 0) {
      deferred.resolve({
        name: name
      });
    } else {
      deferred.reject({
        name: name,
        exitCode: code
      });
    }
  });
  return deferred.promise;
}

module.exports = dotCover;