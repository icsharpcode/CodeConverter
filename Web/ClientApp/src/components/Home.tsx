import React, { useState } from 'react';
import axios from 'axios';

export const Home = () => {
    const defaultVbCode = "Public Class VisualBasicClass\r\n\r\nEnd Class";
    const defaultCsCode = "public class CSharpClass\r\n{\r\n\r\n}";
    const [inputCode, setInputCode] = useState(defaultVbCode);
    const [convertedCode, setConvertedCode] = useState("");
    const [errorMessageOnResponse, setErrorMessageOnResponse] = useState("");
    const [converterCallInFlight, setConverterCallInFlight] = useState(false);
    const vbNetToCsId = "vbnet2cs";
    const [conversionType, setConversionType] = useState(vbNetToCsId);
    const onInputCodeChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => setInputCode(e.target.value);
    const onConvertedCodeChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => setConvertedCode(e.target.value);
    const conversionIsFromVb = conversionType === vbNetToCsId;
    const selectTextArea = (elementId: string) => {
        setTimeout(() => {
            var output = document.getElementById(elementId) as HTMLTextAreaElement;
            output.focus();
            output.select();
        });
    };
    const setDefaultInput = (conversionType: string) => {
        setConversionType(conversionType);
        if (!inputCode || inputCode === defaultVbCode || inputCode === defaultCsCode) {
            setInputCode(conversionType === vbNetToCsId ? defaultVbCode : defaultCsCode);
            setConvertedCode("");
        } else if (convertedCode) {
            setInputCode(convertedCode);
            setConvertedCode(inputCode);
        }
        selectTextArea("inputTextArea");
    };

    const convert = () => {
        setConverterCallInFlight(true);
        setConvertedCode("");
        setErrorMessageOnResponse("");

        axios.post('/api/converter/',
                {
                    code: inputCode,
                    requestedConversion: conversionType
                })
            .then((response: any) => {
                setConverterCallInFlight(false);
                if (200 === response.status) {
                    setConvertedCode(response.data.convertedCode);

                    if (!response.data.conversionOk) {
                        setErrorMessageOnResponse(response.data.errorMessage);
                    }
                }
                selectTextArea("outputTextArea");
            })
            .catch((error: any) => {
                setConverterCallInFlight(false);

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
    };


    return (
            <div id="app">
                <div className="form-group">
                    <label>Input code ({conversionIsFromVb ? "VB.NET" : "C#"})</label>
                    <textarea id="inputTextArea" value={inputCode} onChange={onInputCodeChange} className="form-control" rows={10} style={{ minWidth: "100%" }}></textarea>
                </div>
                <div className="form-group">
                    <button id="convert-button" className="btn btn-default" onClick={() => convert()}>Convert Code</button>
                    &nbsp;
                    <label className="horizontal-spaced">
                        <input type="radio" checked={conversionIsFromVb} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setDefaultInput(e.target.value)} value={vbNetToCsId} />
                        VB.NET to C#
                    </label>
                    &nbsp;
                    <label className="horizontal-spaced">
                        <input type="radio" checked={!conversionIsFromVb} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setDefaultInput(e.target.value)} value="cs2vbnet"/>
                        C# to VB.NET
                    </label>
                    &nbsp;
                    {converterCallInFlight && <div className="spinner horizontal-spaced"></div>}
                </div>
                <div className="form-group">
                    <label>Converted code ({conversionIsFromVb ? "C#" : "VB.NET"})</label>
                    <textarea id="outputTextArea" value={convertedCode} onChange={onConvertedCodeChange} className="form-control" rows={10} style={{ minWidth: "100%" }}></textarea>
                </div>

                {!converterCallInFlight &&
                    errorMessageOnResponse.length > 1 &&
                    <p style={{ whiteSpace: "pre-wrap" }}>Error message:<br/>{errorMessageOnResponse}</p>}

                <p>Get a more accurate conversion by using our free <a href="https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter">Code Converter extension for Visual Studio</a>.</p>
            </div>
        );
};