import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import 'isomorphic-fetch';

interface WorkersState {
    workers: Worker[];
    loading: boolean;
    refreshInterval: number;
}

export class Workers extends React.Component<{}, WorkersState> {
    constructor() {
        super();
        this.refresh = this.refresh.bind(this);
        this.state = { workers: [], loading: true, refreshInterval: setInterval(this.refresh, 5000) };
        this.refresh();
    }

    public refresh() {
        fetch('Home/Workers')
            .then(response => response.json() as Promise<Worker[]>)
            .then(data => {
                this.setState({ workers: data, loading: false });
            });
    }

    public componentWillUnmount() {
        clearInterval(this.state.refreshInterval);
    }

    public render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Workers.renderTable(this.state.workers);

        return <div>
            <h1>Workers</h1>
            {contents}
        </div>;
    }

    private static renderTable(workers: Worker[]) {
        return <table className='table'>
            <thead>
                <tr>
                    <th>Id</th>
                    <th>Queues</th>
                    <th>Job</th>
                </tr>
            </thead>
            <tbody>
                {workers.map((worker, index) =>
                    <tr key={index}>
                        <td>{worker.id}</td>
                        <td>{worker.queues}</td>
                        <td>{worker.job}</td>
                    </tr>
                )}
            </tbody>
        </table>;
    }
}

interface Worker {
    id: string;
    queues: string[];
    last_check_in: string;
    job: JobStatus;
}

interface JobStatus {
    started_at: string;
    class: string;
    args: string[];
    queue: string;
    retries: number;
}