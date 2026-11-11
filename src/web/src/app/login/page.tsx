"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiFetch, type UserInfo } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); // 폼 기본 동작(페이지 새로고침) 막기
    setError("");
    setSubmitting(true);

    const result = await apiFetch<UserInfo>("/api/auth/login", {
      method: "POST",
      body: { email, password },
    });

    setSubmitting(false);
    if (!result.success) {
      setError(result.message);
      return;
    }
    router.push("/"); // 로그인 성공 → 홈으로
  }

  return (
    <main className="flex flex-1 items-center justify-center px-4">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-4">
        <h1 className="text-2xl font-bold">로그인</h1>

        <label className="block">
          <span className="text-sm">이메일</span>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="mt-1 w-full rounded border border-gray-400 bg-transparent px-3 py-2"
          />
        </label>

        <label className="block">
          <span className="text-sm">비밀번호</span>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="mt-1 w-full rounded border border-gray-400 bg-transparent px-3 py-2"
          />
        </label>

        {error && <p className="text-sm text-red-500">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50"
        >
          {submitting ? "로그인 중..." : "로그인"}
        </button>

        <p className="text-center text-sm">
          계정이 없나요?{" "}
          <Link href="/signup" className="text-blue-500 underline">
            회원가입
          </Link>
        </p>
      </form>
    </main>
  );
}
