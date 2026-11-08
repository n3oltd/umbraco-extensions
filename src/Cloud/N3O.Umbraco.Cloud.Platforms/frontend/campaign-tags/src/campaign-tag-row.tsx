import { useId, useState } from 'react';
import { UuiButton, UuiSelect } from '@n3oltd/backoffice-ui';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import { useTagValues } from './use-tag-values';
import type { CampaignTag, TagKey } from './campaign-tags-value';

interface CampaignTagRowProps {
    row: CampaignTag;
    keyOptions: TagKey[];
    authFetch: AuthFetch | null;
    onChange: (row: CampaignTag) => void;
    onRemove: () => void;
}

export function CampaignTagRow({ row, keyOptions, authFetch, onChange, onRemove }: CampaignTagRowProps) {
    const listId = useId();
    const [isActive, setIsActive] = useState(false);
    const { values, error } = useTagValues(authFetch, isActive ? row.key : '', row.value);
    const tagKey = keyOptions.find((option) => option.key === row.key);

    return (
        <div className="row">
            <div className="fields">
                <UuiSelect
                    options={keyOptions.map((option) => ({ name: option.name, value: option.key }))}
                    value={row.key}
                    placeholder="Select a tag…"
                    onChange={(key) => onChange({ key, value: row.value })}
                />
                <input
                    type="text"
                    list={listId}
                    value={row.value}
                    placeholder="Value"
                    aria-label={tagKey ? `${tagKey.name} value` : 'Tag value'}
                    disabled={!row.key}
                    onFocus={() => setIsActive(true)}
                    onChange={(e) => onChange({ key: row.key, value: e.currentTarget.value })}
                />
                <datalist id={listId}>
                    {values.map((value) => (
                        <option key={value} value={value} />
                    ))}
                </datalist>
                <UuiButton label="Remove" look="secondary" compact onClick={onRemove} />
            </div>
            {tagKey?.description ? <p className="description">{tagKey.description}</p> : null}
            {error ? <p className="error">{error}</p> : null}
        </div>
    );
}
