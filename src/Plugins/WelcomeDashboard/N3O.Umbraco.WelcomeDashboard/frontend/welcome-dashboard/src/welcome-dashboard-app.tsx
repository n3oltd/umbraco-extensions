import styles from './welcome-dashboard-app.css?inline';

export function WelcomeDashboardApp() {
    return (
        <uui-box headline="Help & Support">
            <p>
                Please visit the N3O Support Centre to view the latest help articles, documentation and to
                contact our support team with any queries.
            </p>

            <p>
                <a href="https://help.n3o.cloud" target="_blank" rel="noopener">
                    Visit Support Centre &rarr;
                </a>
            </p>

            <style>{styles}</style>
        </uui-box>
    );
}
