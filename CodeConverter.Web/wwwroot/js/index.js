// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification for details on configuring this project to bundle and minify static web assets.

var app = angular.module('ccApp', []);

app.controller('ccController', function ($scope, $timeout, $http) {
    const vbToCsId = "vbnet2cs";
    const defaultVbInput = "Public Class VisualBasicClass\r\n\r\nEnd Class";
    const defaultCsInput = "public class CSharpClass\r\n{\r\n\r\n}";
    $scope.convertedCode = "";
    $scope.requestedConversion = vbToCsId;
    $scope.errorMessageOnResponse = "";
    $scope.showErrors = false;
    $scope.converterCallInflight = false;
    $scope.inputCodeLanguage = function () { return $scope.requestedConversion === vbToCsId ? "VB.NET" : "C#"; };
    $scope.outputCodeLanguage = function () { return $scope.requestedConversion === vbToCsId ? "C#" : "VB.NET"; };

    $scope.setDefaultInput = function () {
        if ($scope.convertedCode) {
            $scope.inputCode = $scope.convertedCode;
        } else if (!$scope.inputCode || $scope.inputCode === defaultVbInput || $scope.inputCode === defaultCsInput) {
            $scope.inputCode = $scope.requestedConversion === vbToCsId ? defaultVbInput : defaultCsInput;
        }
        $scope.convertedCode = "";

        $timeout(function() {
            const input = document.getElementById("inputTextArea");
            input.focus();
            input.select();
        });
    };

    $scope.convert = function () {
        $scope.converterCallInflight = true;
        $scope.convertedCode = "";
        $scope.errorMessageOnResponse = "";
        $scope.showErrors = false;

        var data = JSON.stringify({code: $scope.inputCode, requestedConversion: $scope.requestedConversion });

        $http.post('/api/converter/', data).then(function successCallback(response) {
                $scope.converterCallInflight = false;
                $scope.convertedCode = response.data.convertedCode;

                // Wait for render before selecting output (ready to copy and paste)
                $timeout(function() {
                    const output = document.getElementById("outputTextArea");
                    output.focus();
                    output.select();
                });

                if (!response.data.conversionOk) {
                    $scope.showErrors = true;
                    $scope.errorMessageOnResponse = response.data.errorMessage;
                }
            },
            function errorCallback(response) {
                $scope.converterCallInflight = false;
                $scope.showErrors = true;
                $scope.errorMessageOnResponse = "Call to the server backend failed";
            });
    };
    window.onload = function () {
        // Ctrl-enter converts
        $(document).bind("keydown", function(e) {
            if ((e.ctrlKey || e.metaKey) && (e.keyCode === 13 || e.keyCode === 10)) {
                document.getElementById("convert-button").click();
                e.preventDefault();
            }
        });
        $scope.setDefaultInput();
    };

    $scope.setDefaultInput();
});