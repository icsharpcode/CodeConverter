import * as Api from "./Api"
import React, { Component, useEffect, useState } from "react";
import { Route, Routes } from "react-router";
import Layout from "./components/Layout";
import { Home } from "./components/Home";
import { About } from "./components/About";

import "./custom.css"

const App = () => {
    const [versionString, setVersionString] = useState("");
    useEffect(() => {
        Api.getVersion()
            .then((response: any) => {
                setVersionString(response.data);
            });
    }, []);

    return (
        <Layout>
            <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/about" element={<About />} />
            </Routes>
            <hr />
            <footer>
                <p>	&copy; 2015-{new Date().getFullYear()} - Code Converter {versionString && (<span>{versionString} </span>)}by <a href="https://github.com/icsharpcode/CodeConverter/">https://github.com/icsharpcode/CodeConverter/</a></p>
            </footer>
        </Layout>
    );
};

export default App;