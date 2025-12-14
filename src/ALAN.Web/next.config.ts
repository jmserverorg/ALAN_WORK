import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  serverExternalPackages: ["@copilotkit/runtime"],
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: process.env.CHATAPI_URL || 'http://localhost:5041/api/:path*',
      },
    ];
  },
};

export default nextConfig;
