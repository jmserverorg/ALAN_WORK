import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  serverExternalPackages: ["@copilotkit/runtime"],
//   async rewrites() {
//     return [
//       {
//         source: '/api/:path*',
//         destination: 'http://localhost:5041/api/:path*',
//       },
//     //   {
//     //     source: '/copilotkit/:path*',
//     //     destination: 'http://localhost:5041/copilotkit/:path*',
//     //   },
//     ];
//   },
};

export default nextConfig;
