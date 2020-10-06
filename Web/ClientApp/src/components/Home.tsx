import React, { useState, useRef } from "react";
import axios from "axios";
import ClientSettings from "../ClientSettings.json"
import { ControlledEditor } from "@monaco-editor/react";
import * as monacoEditor from "monaco-editor";

export const Home = () => {
    const defaultVbCode = "Public Class VisualBasicClass\r\n\r\nEnd Class";
    const defaultCsCode = "public class CSharpClass\r\n{\r\n\r\n}";
    const [inputCode, setInputCode] = useState(defaultVbCode);
    const [convertedCode, setConvertedCode] = useState("");
    const [errorMessageOnResponse, setErrorMessageOnResponse] = useState("");
    const [converterCallInFlight, setConverterCallInFlight] = useState(false);
    const vbNetToCsId = "vbnet2cs";
    const [conversionType, setConversionType] = useState(vbNetToCsId);
    const conversionIsFromVb = conversionType === vbNetToCsId;
    const inputEditor = useRef(null as unknown as monacoEditor.editor.IStandaloneCodeEditor);
    const outputEditor = useRef(null as unknown as monacoEditor.editor.IStandaloneCodeEditor);
    const selectAndFocus = (editorToFocus: React.MutableRefObject<monacoEditor.editor.IStandaloneCodeEditor>) => {
        setTimeout(() => {
            const editorToFocusCurrent = editorToFocus.current;
            if (editorToFocusCurrent) {
                editorToFocusCurrent.focus();
                const textModel = editorToFocusCurrent.getModel();
                if (textModel) {
                    editorToFocusCurrent.setSelection(textModel.getFullModelRange());
                }
            }
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
        selectAndFocus(inputEditor);
    };

    const convert = () => {
        setConverterCallInFlight(true);
        setConvertedCode("");
        setErrorMessageOnResponse("");

        axios.post(ClientSettings.endpoints.conversion,
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
                selectAndFocus(outputEditor);
            })
            .catch((error: any) => {
                setConverterCallInFlight(false);
                setErrorMessageOnResponse(error.message);

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
                    console.log("Error", error.message);
                }
                console.log(error.config);
            });
    };

    return (
            <div id="app">
                <div className="form-group">
                    <label>Input code ({conversionIsFromVb ? "VB.NET" : "C#"})</label>
                    <ControlledEditor
                        className="code-editor"
                        value={inputCode} language={conversionIsFromVb ? "vb" : "csharp"} 
                        onChange={(ev, code) => setInputCode(code || "")}
                        editorDidMount={ (_, editor) => { inputEditor.current = editor; selectAndFocus(inputEditor); } }
                        height="30vh"
                        options={ { lineNumbers: false, minimap: { enabled: false }, formatOnPaste: true, formatOnType: true, padding: { top: 10, bottom: 10 }, } }
                    />
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
                    <ControlledEditor
                        className="code-editor"
                        value={convertedCode}
                        language={conversionIsFromVb ? "csharp" : "vb"}
                        onChange={(ev, code) => setConvertedCode(code || "")}
                        editorDidMount={(_, editor) => outputEditor.current = editor}
                        height="30vh"
                        options={ { lineNumbers: false, readOnly: true, minimap: { enabled: false }, glyphMargin: false, padding: { top: 5, bottom: 5 }, } }
                    />
                </div>

                {!converterCallInFlight &&
                    errorMessageOnResponse.length > 1 &&
                    <p style={{ whiteSpace: "pre-wrap" }}>Error message:<br/>{errorMessageOnResponse}</p>}

                <p>Get a more accurate conversion by using our free <a href="https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter">Code Converter extension for Visual Studio</a>.</p>
            </div>
        );
};