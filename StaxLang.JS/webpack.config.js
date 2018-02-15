const path = require('path');
const UglifyJSPlugin = require('uglifyjs-webpack-plugin');
const webpack = require('webpack');

const commitHash = require('child_process')
  .execSync('git rev-parse --short HEAD')
  .toString();

module.exports = {
    entry: './js/ui.js',
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'dist')
    },
    plugins: [
        new webpack.DefinePlugin({
            __COMMIT_HASH__: JSON.stringify(commitHash),
            __BUILD_DATE__: JSON.stringify(new Date),
        }),
        new UglifyJSPlugin
    ]
};
