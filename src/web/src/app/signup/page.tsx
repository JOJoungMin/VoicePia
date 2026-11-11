"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { apiFetch } from "@/lib/api";

export default function SignUpPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [penName, setPenName] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setSubmitting(true);

    const result = await apiFetch<number>("/api/auth/signup", {
      method: "POST",
      body: { email, password, penName },
    });

    setSubmitting(false);
    if (!result.success) {
      setError(result.message); // 예: "이미 가입된 이메일입니다."
      return;
    }
    router.push("/login"); // 가입 성공 → 로그인 화면으로
  }

  return (
    <main className="flex flex-1 items-center justify-center px-4">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-4">
        <h1 className="text-2xl font-bold">회원가입</h1>

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
          <span className="text-sm">비밀번호 (8자 이상)</span>
          <input
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="mt-1 w-full rounded border border-gray-400 bg-transparent px-3 py-2"
          />
        </label>

        <label className="block">
          <span className="text-sm">필명</span>
          <input
            type="text"
            required
            maxLength={50}
            value={penName}
            onChange={(e) => setPenName(e.target.value)}
            className="mt-1 w-full rounded border border-gray-400 bg-transparent px-3 py-2"
          />
        </label>

        {error && <p className="text-sm text-red-500">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50"
        >
          {submitting ? "가입 중..." : "가입하기"}
        </button>

        <p className="text-center text-sm">
          이미 계정이 있나요?{" "}
          <Link href="/login" className="text-blue-500 underline">
            로그인
          </Link>
        </p>
      </form>
    </main>
  );
}
