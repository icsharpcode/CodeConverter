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

            var capturedObj = this;
            axios.post('/api/converter/', {
                code: this.inputCode,
                requestedConversion: this.requestedConversion
            })
            .then(function (response) {
                capturedObj.converterCallInflight = false;
                if (200 === response.status) {
                    capturedObj.convertedCode = response.data.convertedCode;

                    if (!response.data.conversionOk) {
                        capturedObj.showErrors = true;
                        capturedObj.errorMessageOnResponse = response.data.errorMessage;
                    }

                    setTimeout(function () {
                        var output = document.getElementById("outputTextArea");
                        output.focus();
                        output.select();
                    });
                }
            })
            .catch(function (error) {
                capturedObj.converterCallInflight = false;

                // Copied verbatim from https://github.com/axios/axios for debugging purposes only
                if (error.response) {
                    // The request was made and the server responded with a status code
                    // that falls out of the range of 2xx
                    console.log(error.response.data);
                    console.log(error.response.status);
                    console.log(error.response.headers);
                } else if (error.request) {
                    // The request was made but no response was received
                    // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                    // http.ClientRequest in node.js
                    console.log(error.request);
                } else {
                    // Something happened in setting up the request that triggered an Error
                    console.log('Error', error.message);
                }
                console.log(error.config);
            });
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