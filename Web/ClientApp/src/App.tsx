import React, { Component } from 'react';
import { Route } from 'react-router';
import Layout from './components/Layout';
import { Home } from './components/Home';
import { About} from './components/About';

import './custom.css'

export default class App extends Component {

    render () {
        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route exact path='/about' component={About} />
                <hr />
                <footer>
                    <p>	&copy; 2015-{new Date().getFullYear()} - Code Converter by <a href="https://github.com/icsharpcode/CodeConverter/">https://github.com/icsharpcode/CodeConverter/</a></p>
                </footer>
            </Layout>
        );
    }
}