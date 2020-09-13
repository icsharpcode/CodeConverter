import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';

import './custom.css'

export default class App extends Component {

    render () {
        return (
            <Layout>
                <Route exact path='/' component={Home} />
            </Layout>
        );
    }
}