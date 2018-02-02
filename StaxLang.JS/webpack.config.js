const path = require('path');
//const UglifyJSPlugin = require('uglifyjs-webpack-plugin');
const ClosureCompilerPlugin = require('webpack-closure-compiler');

module.exports = {
    entry: './js/ui.js',
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'dist')
    },
    plugins: [
      //new UglifyJSPlugin()
        new ClosureCompilerPlugin({
            jsCompiler: true  
        })
    ]
};
