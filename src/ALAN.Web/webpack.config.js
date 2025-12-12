const path = require('path');

module.exports = {
  entry: './ClientApp/index.tsx',
  output: {
    path: path.resolve(__dirname, 'wwwroot/dist'),
    filename: 'copilot-chat.bundle.js',
    clean: true
  },
  resolve: {
    extensions: ['.ts', '.tsx', '.js', '.jsx'],
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
      {
        test: /\.css$/,
        use: ['style-loader', 'css-loader'],
      },
    ],
  },
  externals: {
    react: 'React',
    'react-dom': 'ReactDOM',
  },
};
