const common = require('./webpack.common');

module.exports = {
    ...common,
    optimization: {
        minimize: false // no minification in dev builds
    }, 
    devtool: 'inline-source-map'
};
