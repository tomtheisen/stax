const path = require('path');
const UglifyJSPlugin = require('uglifyjs-webpack-plugin');
const webpack = require('webpack');

const commitHash = require('child_process')
  .execSync('git rev-parse --short HEAD')
  .toString();

module.exports = {
    entry: './ui.ts',
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'dist')
    },
    resolve: {
        extensions: [".ts", ".tsx", ".js"]
    },
    module: {
        rules: [
            // all files with a `.ts` or `.tsx` extension will be handled by `ts-loader`
            { test: /\.tsx?$/, loader: "ts-loader" },
            { test: /\.md$/, use: [
                { loader: "html-loader" },
                { loader: "markdown-loader" }
            ]}
        ]
    },
    stats: 'errors-only',
    plugins: [
        new webpack.DefinePlugin({
            __COMMIT_HASH__: JSON.stringify(commitHash),
            __BUILD_DATE__: JSON.stringify(new Date),
        }),
        new UglifyJSPlugin({
            uglifyOptions: {
                safari10: true
            }
        })
    ]
};
