import type { NextConfig } from "next";

// C# API 서버 주소 (배포할 때는 환경 변수 API_URL로 바꿔 끼운다)
const apiUrl = process.env.API_URL ?? "http://localhost:5287";

const nextConfig: NextConfig = {
  /* config options here */

  // 브라우저가 보낸 /api/... 요청을 C# API로 그대로 전달한다 (프록시)
  // → 브라우저 입장에서는 모든 요청이 같은 주소(localhost:3000)라서 CORS·쿠키 문제가 없다
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiUrl}/api/:path*`,
      },
    ];
  },

  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
};

export default nextConfig;
