import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import 'isomorphic-fetch';

interface QueuesState {
    queues: Queue[];
    loading: boolean;
    refreshInterval: number;
}

export class Queues extends React.Component<{}, QueuesState> {
    constructor() {
        super();
        this.refresh = this.refresh.bind(this);
        this.state = { queues: [], loading: true, refreshInterval: setInterval(this.refresh, 5000) };
        this.refresh();
    }

    public refresh() {
        fetch('Home/Queues')
            .then(response => response.json() as Promise<Queue[]>)
            .then(data => {
                this.setState({ queues: data, loading: false });
            });
    }

    public componentWillUnmount() {
        clearInterval(this.state.refreshInterval);
    }

    public render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Queues.renderTable(this.state.queues);

        return <div>
            <h1>Queues</h1>
            {contents}
        </div>;
    }

    private static renderTable(queues: Queue[]) {
        return <table className='table'>
            <thead>
                <tr>
                    <th>Queue</th>
                    <th>Count</th>
                </tr>
            </thead>
            <tbody>
                {queues.map((queue, index) =>
                    <tr key={index}>
                        <td>{queue.name}</td>
                        <td>{queue.jobs.length}</td>
                    </tr>
                )}
            </tbody>
        </table>;
    }
}

interface Queue {
    name: string;
    jobs: JobStatus[];
}

interface JobStatus {
    started_at: string;
    class: string;
    args: string[];
    queue: string;
    retries: number;
}