import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UmbAuthFetchMixin } from '@n3oltd/backoffice-core';
import type { AuthFetch } from '@n3oltd/backoffice-core';

interface ContentUrlsRes {
    stagingUrl: string | null;
    productionUrl: string | null;
}

const elementName = 'n3o-platforms-urls-info-app';

@customElement(elementName)
export class N3oPlatformsUrlsInfoAppElement extends UmbAuthFetchMixin(UmbLitElement) {
    @state() private _stagingUrl: string | null = null;
    @state() private _productionUrl: string | null = null;

    #unique: string | null | undefined;

    constructor() {
        super();

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            this.observe(context?.unique, (unique) => {
                this.#unique = unique;

                void this.#loadUrls();
            });
        });
    }

    authFetchChanged(_authFetch: AuthFetch | null): void {
        void this.#loadUrls();
    }

    async #loadUrls(): Promise<void> {
        if (!this.authFetch || !this.#unique) {
            return;
        }

        try {
            const res = await this.authFetch(
                `/umbraco/backoffice/api/PlatformsBackOffice/contentUrls/${this.#unique}`,
                { headers: { Accept: 'application/json' } },
            );

            const data = (await res.json()) as ContentUrlsRes;

            this._stagingUrl = data.stagingUrl ?? null;
            this._productionUrl = data.productionUrl ?? null;
        } catch {
            this._stagingUrl = null;
            this._productionUrl = null;
        }
    }

    #copy(url: string): void {
        void navigator.clipboard.writeText(url);
    }

    override render() {
        if (!this._stagingUrl && !this._productionUrl) {
            return html``;
        }

        return html`
            <umb-workspace-info-app-layout headline="Platform URLs">
                ${this._stagingUrl ? this.#renderRow('Staging', this._stagingUrl) : ''}
                ${this._productionUrl ? this.#renderRow('Production', this._productionUrl) : ''}
            </umb-workspace-info-app-layout>
        `;
    }

    #renderRow(label: string, url: string) {
        return html`
            <div class="url-row">
                <span class="label">${label}</span>
                <a href="${url}" target="_blank" rel="noreferrer" class="url">${url}</a>
                <button class="copy" @click=${() => this.#copy(url)} title="Copy">Copy</button>
            </div>
        `;
    }

    static override styles = css`
        :host { display: block; }

        .url-row {
            display: flex;
            align-items: baseline;
            gap: var(--uui-size-space-3, 8px);
            padding: var(--uui-size-space-4) var(--uui-size-space-5);
            font-size: 0.8125rem;
        }

        .label {
            flex-shrink: 0;
            width: 72px;
            color: var(--uui-color-text-alt);
            font-weight: 600;
        }

        .url {
            flex: 1;
            color: var(--uui-color-interactive);
            word-break: break-all;
            text-decoration: none;
        }

        .url:hover {
            text-decoration: underline;
        }

        .copy {
            flex-shrink: 0;
            background: none;
            border: 1px solid var(--uui-color-border);
            border-radius: 3px;
            cursor: pointer;
            color: var(--uui-color-interactive);
            padding: 2px 8px;
            font-size: 0.75rem;
        }

        .copy:hover {
            background: var(--uui-color-surface-alt);
        }
    `;
}

export default N3oPlatformsUrlsInfoAppElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: N3oPlatformsUrlsInfoAppElement;
    }
}
