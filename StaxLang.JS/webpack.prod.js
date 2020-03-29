const common = require('./webpack.common');
const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
    ...common,
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin({
            terserOptions: {
                output: {
                    comments: false,
                },
            },
            extractComments: false,
        })],
    }
};