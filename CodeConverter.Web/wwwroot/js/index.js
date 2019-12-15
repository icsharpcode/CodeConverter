var vbToCsId = "vbnet2cs";
var defaultVbInput = "Public Class VisualBasicClass\r\n\r\nEnd Class";
var defaultCsInput = "public class CSharpClass\r\n{\r\n\r\n}";

var app = new Vue({
    el: '#app',
    data: {
        inputCode: defaultVbInput,
        requestedConversion: vbToCsId,
        convertedCode: "",
        converterCallInflight: false,
        errorMessageOnResponse: "",
        showErrors: false
    },
    watch: {
        requestedConversion: function (newConversion, oldConversion) {
            this.setDefaultInput();
        }
    },
    computed: {
        inputCodeLanguage: function () {
            return this.requestedConversion === vbToCsId ? "VB.NET" : "C#";
        },
        outputCodeLanguage: function () {
            return this.requestedConversion === vbToCsId ? "C#" : "VB.NET";
        }
    },
    methods: {
        convert: function () {
            this.converterCallInflight = true;
            this.convertedCode = "";
            this.errorMessageOnResponse = "";
            this.showErrors = false;

            var xhttp = new XMLHttpRequest();
            var capturedObj = this;

            xhttp.onreadystatechange = function () {
                if (this.readyState === 4) {
                    capturedObj.converterCallInflight = false;
                }
                if (this.readyState === 4 && this.status === 200) {
                    var response = JSON.parse(this.responseText);
                    capturedObj.convertedCode = response.convertedCode;

                    if (!response.conversionOk) {
                        capturedObj.showErrors = true;
                        capturedObj.errorMessageOnResponse = response.errorMessage;
                    }

                    setTimeout(function () {
                        var output = document.getElementById("outputTextArea");
                        output.focus();
                        output.select();
                    });
                }
            };
            xhttp.open("POST", "/api/converter/", true);
            xhttp.setRequestHeader("Content-Type", "application/json");

            var data = JSON.stringify({ code: this.inputCode, requestedConversion: this.requestedConversion });
            xhttp.send(data);
        },
        setDefaultInput: function () {
            if (!this.inputCode || this.inputCode === defaultVbInput || this.inputCode === defaultCsInput) {
                this.inputCode = this.requestedConversion === vbToCsId ? defaultVbInput : defaultCsInput;
                this.convertedCode = "";
            } else if (this.convertedCode) {
                var tempInput = this.inputCode;
                this.inputCode = this.convertedCode;
                this.convertedCode = tempInput;
            }

            setTimeout(function () {
                var input = document.getElementById("inputTextArea");
                input.focus();
                input.select();
            });
        }
    },
    beforeMount() {
        this.setDefaultInput();

            // Ctrl-enter converts
            document.body.addEventListener('keydown', function (e) {
                if ((e.ctrlKey || e.metaKey) && (e.keyCode === 13 || e.keyCode === 10)) {
                    document.getElementById("convert-button").click();
                    e.preventDefault();
                }
            });
    }
});