jQuery(document).ready(function ($) {
    var emptyGuid = "00000000000000000000000000000000";
/*
    function navigateToLoginPage() {
        var authorizationUrl = "https://" + window.location.host + "/auth/connect/authorize";
        var clientId = "web";
        var redirectUri = "https://" + window.location.host + window.location.pathname;
        var responseType = "id_token";
        var scope = "openid roles all_claims";
        var state = Date.now() + "" + Math.random();
        var nonce = "" + Math.random() + Math.random() + Math.random();

        localStorage["state"] = state;
        localStorage["nonce"] = nonce;

        var url =
            authorizationUrl + "?" +
                "client_id=" + encodeURI(clientId) + "&" +
                "redirect_uri=" + encodeURI(redirectUri) + "&" +
                "response_type=" + encodeURI(responseType) + "&" +
                "scope=" + encodeURI(scope) + "&" +
                "state=" + encodeURI(state) + "&" +
                "nonce=" + encodeURI(nonce);
        window.location = url;
    }

    function processTokenCallback() {
        var hash = window.location.hash.substr(1);
        var result = hash.split("&").reduce(function(result, item) {
            var parts = item.split("=");
            result[parts[0]] = parts[1];
            return result;
        }, {});

        if (!result.error) {
            if (result.state !== localStorage["state"]) {
                // invalid state
            } else {
                localStorage.removeItem("state");
                localStorage.removeItem("nonce");
                return result["id_token"];
            }
        }

        return null;
    }

    var token = "";
    if (window.location.hash) {
        token = processTokenCallback();
        if (token) {
            localStorage["token"] = token;
        }
    } else {
        token = localStorage["token"];
    }

    function setAuthHeader(request) {
        request.setRequestHeader("Authorization", "Bearer " + token);
    }*/

    self.baseUrl = (function () {
        var pathname = window.location.pathname;
        if (pathname[pathname.length - 1] !== "/") {
            pathname += "/";
        }

        return window.location.protocol + "//" + window.location.host + pathname;
    })();

    self.eventEndpoint = self.baseUrl + "invoke";
    self.completionEndpoint = self.baseUrl + "complete/";

    function getPreciseTime() {
        if (performance && performance.now) {
            return performance.now();
        }

        return new Date().getTime();
    }

    function admin() {
        var nextEventId = 0;
        var self = this;
        self.to = "calculator/" + emptyGuid;
        self.shellFn = {};

        self.createEvent = function (type, args) {
            return {
                to: self.to,
                id: ++nextEventId,
                type: type,
                args: args
            };
        };

        self.guid = (function () {
            function s4() {
                return Math.floor((1 + Math.random()) * 0x10000)
                           .toString(16)
                           .substring(1);
            }
            return function () {
                return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
            };
        })();

        self.req = function (type, value) {

            return $.ajax({
                method: "POST",
                url: self.eventEndpoint,
                data: JSON.stringify(self.createEvent(type, value)),
                contentType: "application/json" /*,
                beforeSend: setAuthHeader*/
            }).then(function (result) {
                return result;
            }, function (error) {
                // Perform login if access is denied.
                if (error.status === 403) {
                    /*navigateToLoginPage();*/
                }

                return error.responseJSON || error;
            });
        };

        self.shellFn.req = function (cmd) {
            var type = cmd.args[0];
            var args = cmd.args.slice(1);
            self.reqJs(self.req(type, args));
        };

        self.reqJs = function (request) {
            var startTime = getPreciseTime();
            var printedResult = false;
            request.then(
                function (next) {
                    printedResult = true;
                    window.terminal.echo(JSON.stringify(next, null, "  "));
                    window.terminal.echo((getPreciseTime() - startTime).toFixed(2) + "ms");
                    window.terminal.resume();
                },
                function (error) {
                    printedResult = true;
                    var msg = error;
                    if (typeof error === "object") {
                        msg = JSON.stringify(error, null, "  ");
                    }
                    window.terminal.error("Error: " + msg);
                    window.terminal.echo((getPreciseTime() - startTime).toFixed(2) + "ms");
                    window.terminal.resume();
                },
                function () {
                    if (!printedResult) {
                        window.terminal.echo("Done. " + (getPreciseTime() - startTime).toFixed(2) + "ms");
                    }
                    window.terminal.resume();
                });
        };

        self.shellFn.js = function (command) {
            // Evaluate commands of the form "<method>(<arg*>)".
            var i = command.indexOf("(");
            if (i < 0) {
                i = command.length;
                command += "()";
            }

            var method = command.substr(0, i);
            var args = command.substr(i + 1).replace(/\){1};{0,1}$/, "");
            if (args) {
                args = ",[" + args + "]";
            }

            var result = window.eval("self.reqJs(self.req(\"" + method + "\"" + args + "));");
            if (result != undefined) {
                if (typeof result === "object") {
                    window.terminal.echo(JSON.stringify(result, null, "  "));
                } else {
                    window.terminal.echo(String(result));
                }
            }
        };

        self.complete = function (terminal, commandIgnored, callback) {
            var command = terminal.get_command();
            var cmd = $.terminal.parse_command(command);
            if (cmd.name === "to") {
                if (cmd.args.length === 1) {
                    var arg = cmd.args[0];
                    if (arg[arg.length - 1] === "/") {
                        callback(["to " + arg + emptyGuid]);
                        return;
                    }
                }

                $.get(self.completionEndpoint + "kind/" + (cmd.args[0] || "")).then(function (val) {
                    var results = [];
                    for (var i = 0; i < val.length; ++i) {
                        results.push(val[i]);
                    }

                    callback(results);
                });
            } else {
                var kind = self.to.substr(0, self.to.indexOf("/"));
                var partial = { kind: kind, cmd: cmd.name, args: cmd.args };
                $.ajax({
                    method: "POST",
                    url: self.completionEndpoint + "command",
                    data: JSON.stringify(partial),
                    contentType: "application/json"
                }).then(function (val) {
                    for (var key in self.commands) {
                        if (self.commands.hasOwnProperty(key) && key !== "to") {
                            val.push(key);
                        }
                    }
                    callback(val);
                });
            }
        };

        self.commands = [];

        self.getHandler = function (name) {
            var c = self.commands[name];
            if (typeof c === "string") {
                return self.getHandler(c);
            } else if (c) {
                return c.fn;
            }

            return null;
        };

        self.handleCommand = function (command, terminal) {
            command = command.trim();
            if (command.length === 0) {
                return;
            }

            window.terminal = terminal;
            var cmd = $.terminal.parse_command(command);
            var handler = getHandler(cmd.name);
            if (handler) {
                handler(cmd);
            } else if (command.match(/^[$A-Z_][0-9A-Z_$]*($|\s)/i)) {
                // Let users skip the 'req' bit if an unknown command looks like a request.
                self.shellFn.req($.terminal.parse_command("req " + command));
            } else {
                // Interpret all other commands as JavaScript.
                window.terminal.error("Unknown command. Try 'js' for a JavaScript terminal, or 'help' for a list of commands.");
            }
        };

        self.usage = function () {
            var result = "Usage:";
            for (var i in self.commands) {
                if (commands.hasOwnProperty(i)) {
                    var cmd = commands[i];

                    var u;
                    var h = "";
                    if (typeof cmd === "string") {
                        u = "alias for " + cmd + ".";
                    } else {
                        u = cmd.usage;
                        if (cmd.help) {
                            h = " - " + cmd.help;
                        }
                    }

                    result += "\n\t" + i + " - " + u + h;
                }
            }

            result += "\n** Use tab completion. **";

            return result;
        };

        var systemMethods = {};
        (function () {
            $.ajax({
                method: "GET",
                url: self.baseUrl + "complete/actors",
                contentType: "application/json"
                /*beforeSend: setAuthHeader*/
            }).then(function (result) { systemMethods = result });
            $.ajax({
                method: "GET",
                url: self.baseUrl + "id",
                contentType: "application/json", /*
                beforeSend: setAuthHeader*/
            }).then(function (result) {
                window.terminal.set_prompt(result + "> ");
                self.to = result;
            }, function (error) {
                if (console && console.log) console.log(error);
                /*navigateToLoginPage();*/
            });
        })();
        self.help = function () {
            if (self.to.indexOf("/") < 0) {
                return;
            }

            var kind = self.to.substr(0, self.to.indexOf("/"));
            if (kind !== "") {
                var methods = systemMethods[kind].methods;
                if (methods) {
                    for (var method in methods) {
                        if (methods.hasOwnProperty(method)) {

                            // Get arguments.
                            var args = "";
                            for (var arg = 0; arg < methods[method].args.length; arg++) {
                                var methodArg = methods[method].args[arg];
                                if (args) {
                                    args += ", ";
                                }

                                args += methodArg.type + " " + methodArg.name;
                            }

                            // Get method help.
                            window.terminal.echo((methods[method].returnType || "void") + " " + methods[method].name + "(" + args + ")");
                        }
                    }
                }
            }
        }

        self.stateStack = [];
        self.commands = {
            guid: {
                usage: "guid",
                help: "returns a new guid",
                fn: function () {
                    window.terminal.echo(guid());
                }
            },
            qr: {
                usage: "qr",
                help: "returns a qr for current object.",
                fn: function () {
                    window.terminal.echo('<img src="/api/qr/' + self.to.split("/")[1] + '" >',
                    {
                        raw: true
                    });
                }
            },
            req: {
                usage: "req <event type> [<event args>] [<from address>]",
                help: "sends a request.",
                fn: self.shellFn.req
            },
            usage: {
                usage: "usage",
                fn: function () {
                    window.terminal.echo(self.usage());
                }
            },
            help: {
                usage: "help",
                fn: self.help
            },
            js: {
                usage: "js [cmd]",
                help: "enter a JavaScript command prompt if no argument is provided. If arguments are provided, evaluates the arguments.",
                fn: function (cmd) {
                    if (!cmd.args || cmd.args.length === 0) {
                        // Enter JS console.
                        var name = self.to + " js";
                        window.terminal.push(
                            self.shellFn.js,
                            {
                                prompt: name + "> ",
                                name: name
                            });
                    } else {
                        // Eval one command.
                        self.shellFn.js(cmd.args.join(" "));
                    }
                }
            },
            to: {
                usage: "to <address>",
                help: "set the new command target, eg some item (to item/<guid>), some chat room, sanic (to sanic), etc.",
                fn: function (cmd) {
                    var addr = cmd.args[0];
                    if (addr.indexOf("/") <= 0) {
                        addr += "/";
                    }

                    if (addr[addr.length - 1] === "/") {
                        addr += emptyGuid;
                    }

                    if (addr.indexOf("sanic") > -1) {
                        window.terminal.echo($("#sanic-tpl").html(), { raw: 1 });
                    }
                    window.terminal.push(
                        handleCommand,
                        {
                            prompt: addr + "> ",
                            name: addr,
                            onStart: function () {
                                self.stateStack.push(self.to);
                                self.to = addr;
                            },
                            onExit: function () {
                                self.to = self.stateStack.pop();
                            },
                            completion: self.complete
                        });
                }
            }
        };

        return self;
    }

   /* if (!token) {
        navigateToLoginPage();
    }*/

    var shell = admin();
    window.terminal = $("#terminal").terminal(shell.handleCommand, {
        prompt: shell.to + "> ",
        name: shell.to,
        completion: shell.complete,
        greetings:
"______                 _           _                " + "\n" +
"|  _  \\               | |         | |          " + "Type 'help' for some available commands.\n" +
"| | | |__ _ _ __  _ __| |     __ _| |__  ___   " + "Use tab-completion for assistance.\n" +
"| | | / _` | '_ \\| '__| |    / _` | '_ \\/ __|  " + "\n" +
"| |/ / (_| | |_) | |  | |___| (_| | |_) \\__ \\  " + "\n" +
"|___/ \\__,_| .__/|_|  \\_____/\\__,_|_.__/|___/    " + "\n" +
"           | |                                      " + "\n" +
"           |_|                                      ",
        onBlur: function () {
            // prevent loosing focus
            return false;
        }
    });
});