import { UmbElementMixin, type UmbElement } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT, type UmbOpenApiConfiguration } from '@umbraco-cms/backoffice/auth';

export type AuthFetch = (input: string, init?: RequestInit) => Promise<Response>;

type Constructor<T = object> = new (...args: any[]) => T;

export function createAuthFetch(config: UmbOpenApiConfiguration): AuthFetch {
    return async (input, init = {}) => {
        const token = await config.token();
        const headers = new Headers(init.headers);

        if (token) {
            headers.set('Authorization', `Bearer ${token}`);
        }

        return fetch(input, { ...init, credentials: config.credentials, headers });
    };
}

export const UmbAuthFetchMixin = <T extends Constructor<UmbElement>>(superClass: T) =>
    class extends superClass {
        authFetch: AuthFetch | null = null;

        authFetchChanged?(authFetch: AuthFetch | null): void;

        constructor(...args: any[]) {
            super(...args);

            this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
                this.authFetch = authContext ? createAuthFetch(authContext.getOpenApiConfiguration()) : null;
                this.authFetchChanged?.(this.authFetch);
            });
        }
    };

export { UmbElementMixin };
