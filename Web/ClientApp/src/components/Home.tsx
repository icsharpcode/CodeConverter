import { connect } from "react-redux";

const Home = (props) => (
    <div className="row">
        <div className="col-md-8">
            <div id="app">
                <div className="form-group">
                    <label>Input code ({props.InputCodeLanguage})</label>
                    <textarea id="inputTextArea" v-model="inputCode" className="form-control" rows={10} style={{minWidth: "100%"}}></textarea>
                </div>
                <div className="form-group">
                    <button id="convert-button" className="btn btn-default" onClick={convert()}>Convert Code</button>
                    &nbsp;
                    <label className="horizontal-spaced">
                        <input type="radio" v-model="requestedConversion" value="vbnet2cs" />
                        VB.NET to C#
                    </label>
                    &nbsp;
                    <label className="horizontal-spaced">
                        <input type="radio" v-model="requestedConversion" value="cs2vbnet" />
                        C# to VB.NET
                    </label>
                    &nbsp;
                    {props.ConverterCallInFlight && <div className="spinner horizontal-spaced"></div>}
                </div>
                <div className="form-group">
                    <label>Converted code ({props.OutputCodeLanguage})</label>
                    <textarea id="outputTextArea" v-model="convertedCode" className="form-control" rows={10} style={{ minWidth: "100%" }}></textarea>
                </div>

                {props.ShowErrors && <p style={{ whiteSpace: "pre-wrap" }}>Error message:<br />{props.ErrorMessageOnResponse}</p>}

                <p>Get a more accurate conversion by using our free <a href="https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter">Code Converter extension for Visual Studio</a>.</p>
            </div>
        </div>
    </div>
);

export default connect()(Home);