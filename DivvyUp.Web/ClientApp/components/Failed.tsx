import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import 'isomorphic-fetch';

interface FetchDataExampleState {
    failedJobs: FailedJob[];
    loading: boolean;
    refreshInterval: number;
}

export class Failed extends React.Component<{}, FetchDataExampleState> {
    constructor() {
        super();
        this.refresh = this.refresh.bind(this);
        this.state = { failedJobs: [], loading: true, refreshInterval: setInterval(this.refresh, 5000) };
    }

    public refresh() {
        fetch('Home/Failed')
            .then(response => response.json() as Promise<FailedJob[]>)
            .then(data => {
                this.setState({ failedJobs: data, loading: false });
            });
    }

    public render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Failed.renderForecastsTable(this.state.failedJobs);

        return <div>
            <h1>Failed Jobs</h1>
            {contents}
        </div>;
    }

    private static renderForecastsTable(failedJobs: FailedJob[]) {
        return <table className='table'>
            <thead>
                <tr>
                    <th>Queue</th>
                    <th>Job</th>
                    <th>Arguments</th>
                    <th>Error</th>
                </tr>
            </thead>
            <tbody>
                {failedJobs.map((failure, index) =>
                    <tr key={index}>
                        <td>{failure.work.queue}</td>
                        <td>{failure.work.class}</td>
                        <td>{failure.work.args}</td>
                        <td>{failure.message}</td>
                    </tr>
                )}
            </tbody>
        </table>;
    }
}

interface FailedJob {
    work: Job;
    worker: string;
    message: string;
    backtrace: string[];
}

interface Job {
    class: string;
    args: string[];
    queue: string;
}