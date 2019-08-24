const common = require('./webpack.common');
const UglifyJsPlugin = require('uglifyjs-webpack-plugin');

module.exports = {
    ...common,
    optimization: {
        minimize: true,
        minimizer: [new UglifyJsPlugin({
             uglifyOptions:{
                 safari10: true
             }
        })], 
    }
};