-- 001: 회원 테이블
-- 실행 위치: voicepia DB (DBeaver에서 탭 이름이 <voicepia> 인지 확인)
-- 실행 방법: 이 파일 전체를 열고 Alt+X (스크립트 실행)

CREATE TABLE users (
    user_id            bigint       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email              varchar(256) NOT NULL,
    password_hash      varchar(256) NOT NULL,
    pen_name           varchar(50)  NOT NULL,
    email_confirmed    boolean      NOT NULL DEFAULT false,
    failed_login_count int          NOT NULL DEFAULT 0,
    lockout_end        timestamptz,
    security_stamp     varchar(64)  NOT NULL,
    last_login_at      timestamptz,
    created_at         timestamptz  NOT NULL DEFAULT now(),
    updated_at         timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_users_email       UNIQUE (email),
    CONSTRAINT uq_users_pen_name    UNIQUE (pen_name),
    CONSTRAINT ck_users_email_lower CHECK (email = lower(email))
);

-- postgres 계정으로 만들었을 때 앱 계정이 접근할 수 있도록 주인을 넘긴다
ALTER TABLE users OWNER TO voicepia_app;
