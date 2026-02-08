import * as Api from "../Api"
import React, { useState, useRef, useEffect } from "react";
import  Editor  from "@monaco-editor/react";
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
    const setDefaultInput = (type: string) => {
        setConversionType(type);
        if (!inputCode || inputCode === defaultVbCode || inputCode === defaultCsCode) {
            setInputCode(type === vbNetToCsId ? defaultVbCode : defaultCsCode);
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

        Api.convert(inputCode, conversionType)
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

    const commonEditorOptions: monacoEditor.editor.IEditorConstructionOptions = {
        lineNumbers: "off",
        glyphMargin: false,
        folding: false,
        // Undocumented see https://github.com/Microsoft/vscode/issues/30795#issuecomment-410998882
        lineDecorationsWidth: 2,
        lineNumbersMinChars: 0,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
    };

    return (
        <div id="app">
            <div className="form-group">
                <label>Input code ({conversionIsFromVb ? "VB.NET" : "C#"})</label>
                <Editor
                    className="code-editor"
                    value={inputCode} language={conversionIsFromVb ? "vb" : "csharp"}
                    onChange={(code, _) => setInputCode(code || "")}
                    onMount={ (editor, _) => {
                            inputEditor.current = editor;
                            selectAndFocus(inputEditor);
                        } }
                    height="30vh"
                    options={{ ...commonEditorOptions }}/>
            </div>
            <div className="form-group conversion-controls">
                <label className="horizontal-spaced">
                    <input type="radio" checked={conversionIsFromVb} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setDefaultInput(e.target.value)} value={vbNetToCsId}/>
                    VB.NET to C#
                </label>
                <label className="horizontal-spaced">
                    <input type="radio" checked={!conversionIsFromVb} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setDefaultInput(e.target.value)} value="cs2vbnet"/>
                    C# to VB.NET
                </label>
                <button id="convert-button" className="btn btn-default horizontal-spaced" onClick={() => convert()}>Convert Code</button>
            </div>
            <div className="form-group">
                <label>Converted code ({conversionIsFromVb ? "C#" : "VB.NET"})</label>
                <div className="spinner-container">
                    <Editor
                    className={converterCallInFlight ? "code-editor spinner" : "code-editor"}
                        value={convertedCode}
                        language={conversionIsFromVb ? "csharp" : "vb"}
                        onChange={(code, _) => setConvertedCode(code || "")}
                        onMount={(editor, _) => outputEditor.current = editor}
                        height="30vh"
                            options={{ ...commonEditorOptions, readOnly: true }} />
                </div>
            </div>
            {!converterCallInFlight &&
                errorMessageOnResponse.length > 1 &&
                <p style={{ whiteSpace: "pre-wrap" }}>Error message:<br/>{errorMessageOnResponse}</p>}

            <p>Get a more accurate conversion by using our free <a href="https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter">Code Converter extension for Visual Studio</a>.</p>
        </div>
    );
};
